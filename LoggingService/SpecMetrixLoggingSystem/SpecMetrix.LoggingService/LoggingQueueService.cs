using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using LoggingService.Extensions.Interfaces;
using SpecMetrix.Interfaces;               // ILogEntry
using SpecMetrix.Shared.Logging;           // LogEntry

namespace SpecMetrix.LoggingService.Services
{
    /// <summary>
    /// Single instance queue: controller enqueues logs fast; a background consumer
    /// writes them to Serilog (Mongo sink via your Serilog config).
    /// </summary>
    public sealed class LoggingQueueService : BackgroundService, ILoggingService
    {
        private readonly Channel<ILogEntry> _channel;
        private readonly ILogger<LoggingQueueService> _hostLogger;
        private readonly LoggingIngestionOptions _options;

        public LoggingQueueService(
            ILogger<LoggingQueueService> hostLogger,
            IOptions<LoggingIngestionOptions> options)
        {
            _hostLogger = hostLogger ?? throw new ArgumentNullException(nameof(hostLogger));
            _options = options?.Value ?? new LoggingIngestionOptions();

            _channel = Channel.CreateBounded<ILogEntry>(new BoundedChannelOptions(_options.Capacity)
            {
                FullMode = _options.DropOldest ? BoundedChannelFullMode.DropOldest : BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });
        }

        public void EnqueueLog(ILogEntry logEntry)
        {
            if (logEntry == null) return;

            // Non-blocking: queue behavior controlled by options
            if (!_channel.Writer.TryWrite(logEntry))
            {
                var eventId = ResolveEventId(logEntry);
                var level = ResolveLevel(logEntry);

                // Never drop critical config ingestion errors: sync fallback write
                if (_options.NeverDropCritical &&
                    !string.IsNullOrWhiteSpace(eventId) &&
                    eventId.StartsWith(_options.CriticalEventPrefix, StringComparison.OrdinalIgnoreCase) &&
                    (level == LogEventLevel.Error || level == LogEventLevel.Fatal))
                {
                    try
                    {
                        WriteToSerilog(logEntry);
                        return;
                    }
                    catch (Exception ex)
                    {
                        _hostLogger.LogError(ex, "Critical log fallback write failed (EventId={EventId}).", eventId);
                    }
                }

                _hostLogger.LogWarning("Log queue full; dropping entry. EventId={EventId}", eventId ?? "");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _hostLogger.LogInformation("LoggingQueueService started.");

            try
            {
                await foreach (var entry in _channel.Reader.ReadAllAsync(stoppingToken))
                {
                    try
                    {
                        WriteToSerilog(entry);
                    }
                    catch (Exception ex)
                    {
                        _hostLogger.LogError(ex, "Failed to write log entry.");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // normal on shutdown
            }
            finally
            {
                _hostLogger.LogInformation("LoggingQueueService stopping.");
            }
        }

        private static void WriteToSerilog(ILogEntry e)
        {
            // If the caller uses the shared DTO, we can access rich fields.
            if (e is LogEntry dto)
            {
                var level = MapLevel(dto.Level);
                var eventId = ResolveEventId(dto);

                var logger = Log.ForContext("Namespace", dto.Namespace ?? "SA")
                                .ForContext("EventId", eventId ?? "")
                                .ForContext("MachineName", dto.MachineName ?? "")
                                .ForContext("Code", dto.Code)
                                .ForContext("Process", dto.Process ?? "")
                                .ForContext("ClassMethod", dto.ClassMethod ?? "")
                                .ForContext("Source", dto.Source ?? "")
                                .ForContext("Category", dto.Category.ToString());

                // Attach Metadata/TemplateValues when present
                if (dto.Metadata is { Count: > 0 })
                    logger = logger.ForContext("Metadata", dto.Metadata, destructureObjects: true);

                if (dto.TemplateValues is { Count: > 0 })
                    logger = logger.ForContext("TemplateValues", dto.TemplateValues, destructureObjects: true);

                // IMPORTANT:
                // Do NOT pass dictionary template values as Serilog template args; it will not bind as expected.
                // Keep structure in Metadata/TemplateValues and log a rendered message string.
                var msg = ResolveRenderedMessage(dto);

                if (!string.IsNullOrWhiteSpace(msg))
                {
                    logger.Write(level, "{Message}", msg);
                }
                else
                {
                    // Last resort: dump the whole object
                    logger.Write(level, "{@Entry}", dto);
                }

                return;
            }

            // Generic fallback for any ILogEntry (unknown runtime type)
            loggerFallback(e);
        }

        private static void loggerFallback(ILogEntry e)
        {
            var lvl = ResolveLevel(e);
            var eventId = ResolveEventId(e);

            Log.ForContext("EntryType", e.GetType().FullName ?? "Unknown")
               .ForContext("EventId", eventId ?? "")
               .Write(lvl, "{@Entry}", e);
        }

        private static LogEventLevel ResolveLevel(ILogEntry e)
        {
            try
            {
                if (e is LogEntry dto) return MapLevel(dto.Level);

                var levelProp = e.GetType().GetProperty("Level");
                if (levelProp == null) return LogEventLevel.Information;

                var v = levelProp.GetValue(e, null);
                if (v is SpecMetrix.Interfaces.LogLevel sharedLevel) return MapLevel(sharedLevel);

                if (v is string s && Enum.TryParse<SpecMetrix.Interfaces.LogLevel>(s, true, out var parsed))
                    return MapLevel(parsed);
            }
            catch { }

            return LogEventLevel.Information;
        }

        private static string? ResolveEventId(ILogEntry e)
        {
            // Preferred: first-class EventId property
            try
            {
                var p = e.GetType().GetProperty("EventId");
                if (p != null)
                {
                    var v = p.GetValue(e, null) as string;
                    if (!string.IsNullOrWhiteSpace(v)) return v;
                }
            }
            catch { }

            // Fallback: Metadata["eventId"]
            if (e is LogEntry dto && dto.Metadata != null)
            {
                if (dto.Metadata.TryGetValue("eventId", out var ev) && ev != null)
                {
                    var s = ev.ToString();
                    if (!string.IsNullOrWhiteSpace(s)) return s;
                }
            }

            return null;
        }

        private static string? ResolveRenderedMessage(LogEntry dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.Message)) return dto.Message;
            if (!string.IsNullOrWhiteSpace(dto.RenderedMessage)) return dto.RenderedMessage;

            // If we have a template, try to render it deterministically (best-effort).
            if (!string.IsNullOrWhiteSpace(dto.MessageTemplate) &&
                dto.TemplateValues is { Count: > 0 })
            {
                var rendered = dto.MessageTemplate;
                foreach (var kv in dto.TemplateValues)
                {
                    rendered = rendered.Replace("{" + kv.Key + "}", kv.Value?.ToString() ?? string.Empty);
                }
                return rendered;
            }

            if (!string.IsNullOrWhiteSpace(dto.MessageTemplate)) return dto.MessageTemplate;

            return null;
        }

        private static LogEventLevel MapLevel(SpecMetrix.Interfaces.LogLevel level)
        {
            switch (level)
            {
                case SpecMetrix.Interfaces.LogLevel.Trace: return LogEventLevel.Verbose;
                case SpecMetrix.Interfaces.LogLevel.Debug: return LogEventLevel.Debug;
                case SpecMetrix.Interfaces.LogLevel.Information: return LogEventLevel.Information;
                case SpecMetrix.Interfaces.LogLevel.Warning: return LogEventLevel.Warning;
                case SpecMetrix.Interfaces.LogLevel.Error: return LogEventLevel.Error;
                case SpecMetrix.Interfaces.LogLevel.Critical: return LogEventLevel.Fatal;
                default: return LogEventLevel.Information;
            }
        }
    }
}
