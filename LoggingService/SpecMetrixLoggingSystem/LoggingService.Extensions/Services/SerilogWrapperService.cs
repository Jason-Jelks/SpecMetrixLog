using System;
using System.Collections.Generic;
using LoggingService.Extensions.Interfaces;
using SpecMetrix.Interfaces;
using SpecMetrix.Shared.Logging;

namespace LoggingService.Extensions.Services
{
    public class SerilogWrapperService : ISerilogWrapper
    {
        public void Log(ILogEntry logEntry)
        {
            if (logEntry == null) return;

            var eventId = ResolveEventId(logEntry);

            var logger = Serilog.Log.ForContext("Namespace", logEntry.Namespace ?? string.Empty)
                                    .ForContext("EventId", eventId ?? string.Empty)
                                    .ForContext("Code", logEntry.Code)
                                    .ForContext("Process", logEntry.Process)
                                    .ForContext("Category", logEntry.Category)
                                    .ForContext("Source", logEntry.Source)
                                    .ForContext("DeviceName", logEntry.DeviceName)
                                    .ForContext("MachineName", logEntry.MachineName ?? string.Empty)
                                    .ForContext("ClassMethod", logEntry.ClassMethod);

            if (logEntry.TemplateValues != null && logEntry.TemplateValues.Count > 0)
            {
                logger = logger.ForContext("TemplateValues", logEntry.TemplateValues, destructureObjects: true);
            }

            if (logEntry.Metadata != null && logEntry.Metadata.Count > 0)
            {
                logger = logger.ForContext("Metadata", logEntry.Metadata, destructureObjects: true);
            }

            // Render message deterministically. Do NOT pass dictionary values as Serilog args:
            // - ordering is undefined
            // - binding won't match template property names
            var renderedMessage = RenderMessageTemplate(logEntry.MessageTemplate, logEntry.TemplateValues, logEntry.Message);
            if (string.IsNullOrWhiteSpace(renderedMessage))
            {
                renderedMessage = logEntry.MessageTemplate ?? string.Empty;
            }

            switch (logEntry.Level)
            {
                case LogLevel.Critical:
                    logger.Fatal("{Message}", renderedMessage);
                    break;
                case LogLevel.Error:
                    logger.Error("{Message}", renderedMessage);
                    break;
                case LogLevel.Warning:
                    logger.Warning("{Message}", renderedMessage);
                    break;
                case LogLevel.Debug:
                    logger.Debug("{Message}", renderedMessage);
                    break;
                case LogLevel.Trace:
                    logger.Verbose("{Message}", renderedMessage);
                    break;
                default:
                    logger.Information("{Message}", renderedMessage);
                    break;
            }
        }

        private static string? ResolveEventId(ILogEntry e)
        {
            // Prefer first-class EventId if present (new .NET services)
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
            if (e.Metadata != null &&
                e.Metadata.TryGetValue("eventId", out var ev) &&
                ev != null)
            {
                var s = ev.ToString();
                if (!string.IsNullOrWhiteSpace(s)) return s;
            }

            return null;
        }

        private static string RenderMessageTemplate(string? template, IDictionary<string, object>? values, string? originalMessage)
        {
            if (string.IsNullOrWhiteSpace(template) || values == null || values.Count == 0)
                return originalMessage ?? string.Empty;

            foreach (var value in values)
                template = template.Replace("{" + value.Key + "}", value.Value?.ToString() ?? string.Empty);

            return template;
        }
    }
}
