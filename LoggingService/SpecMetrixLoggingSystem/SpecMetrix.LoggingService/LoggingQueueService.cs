using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using LoggingService.Extensions.Interfaces;
using SpecMetrix.Interfaces;               // ILogEntry
using SpecMetrix.Shared.Logging;           // LogLevel, LogEntry

namespace SpecMetrix.LoggingService.Services
{
    /// <summary>
    /// Single instance queue: controller enqueues logs fast; a background consumer
    /// writes them to Serilog (Mongo sink via your Serilog config).
    /// </summary>
    public sealed class LoggingQueueService : BackgroundService, ILoggingService
    {
        // Bounded channel to protect memory in bursty scenarios
        private readonly Channel<ILogEntry> _channel =
            Channel.CreateBounded<ILogEntry>(new BoundedChannelOptions(10_000)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });

        private readonly ILogger<LoggingQueueService> _hostLogger;

        public LoggingQueueService(ILogger<LoggingQueueService> hostLogger)
        {
            _hostLogger = hostLogger;
        }

        public void EnqueueLog(ILogEntry logEntry)
        {
            if (logEntry == null) return;

            // Non-blocking; drops oldest when full (configured above)
            if (!_channel.Writer.TryWrite(logEntry))
            {
                _hostLogger.LogWarning("Log queue full; dropping oldest entry.");
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
            // If the caller already uses the shared DTO, we can access rich fields.
            if (e is LogEntry dto)
            {
                var level = MapLevel(dto.Level);
                var logger = Log.ForContext("Namespace", dto.Namespace ?? "SA")
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

                // Prefer message template + values; otherwise use rendered/message
                if (!string.IsNullOrWhiteSpace(dto.MessageTemplate) && dto.TemplateValues is { Count: > 0 })
                {
                    // Render with values as a single object so Serilog keeps structure
                    logger.Write(level, dto.MessageTemplate, dto.TemplateValues);
                }
                else if (!string.IsNullOrWhiteSpace(dto.Message))
                {
                    logger.Write(level, "{Message}", dto.Message);
                }
                else if (!string.IsNullOrWhiteSpace(dto.RenderedMessage))
                {
                    logger.Write(level, "{Message}", dto.RenderedMessage);
                }
                else
                {
                    // Last resort: dump the whole object
                    logger.Write(level, "{@Entry}", dto);
                }

                return;
            }

            // Generic fallback for any ILogEntry (unknown runtime type)
            // Serialize the object graph to preserve detail.
            loggerFallback(e);
        }

        private static void loggerFallback(ILogEntry e)
        {
            var lvl = LogEventLevel.Information;
            try
            {
                // If the interface exposes Level, try to map it
                var levelProp = e.GetType().GetProperty("Level");
                if (levelProp != null)
                {
                    var v = levelProp.GetValue(e, null);
                    if (v is SpecMetrix.Interfaces.LogLevel sharedLevel) lvl = MapLevel(sharedLevel);
                    else if (v is string s && Enum.TryParse<SpecMetrix.Interfaces.LogLevel>(s, true, out var parsed))
                        lvl = MapLevel(parsed);
                }
            }
            catch { /* ignore */ }

            Log.ForContext("EntryType", e.GetType().FullName ?? "Unknown")
               .Write(lvl, "{@Entry}", e);
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
