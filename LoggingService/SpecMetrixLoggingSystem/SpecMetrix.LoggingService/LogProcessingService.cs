using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SpecMetrix.Interfaces;
using Serilog;

namespace LoggingService
{
    public class LogProcessingService : BackgroundService, ILoggingService
    {
        private readonly ILogger<LogProcessingService> _logger;

        public LogProcessingService(ILogger<LogProcessingService> logger, MongoLogService mongoLogService)
        {
            _logger = logger;

            // Ensure MongoDB is initialized with the correct settings
            mongoLogService.EnsureDatabaseAndCollection();
        }

        /// <summary>
        /// Logs messages using Serilog, with deduplication handled externally.
        /// </summary>
        public void EnqueueLog(ILogEntry logEntry)
        {
            logEntry.Message = RenderMessageTemplate(logEntry.MessageTemplate, logEntry.TemplateValues, logEntry.Message);

            // Create the logger context with the dynamic namespace and additional context properties
            var logger = Log.ForContext(new NamespaceEnricher(logEntry.Namespace ?? string.Empty))
                            .ForContext("Code", logEntry.Code)
                            .ForContext("Process", logEntry.Process)
                            .ForContext("Category", logEntry.Category)
                            .ForContext("Source", logEntry.Source)
                            .ForContext("DeviceName", logEntry.DeviceName)
                            .ForContext("MachineName", logEntry.MachineName ?? string.Empty)
                            .ForContext("ClassMethod", logEntry.ClassMethod)
                            .ForContext("TemplateValues", logEntry.TemplateValues);

            switch (logEntry.Level)
            {
                case SpecMetrix.Interfaces.LogLevel.Critical:
                    logger.Fatal(logEntry.Message);
                    break;
                case SpecMetrix.Interfaces.LogLevel.Error:
                    logger.Error(logEntry.Message);
                    break;
                case SpecMetrix.Interfaces.LogLevel.Warning:
                    logger.Warning(logEntry.Message);
                    break;
                case SpecMetrix.Interfaces.LogLevel.Debug:
                    logger.Debug(logEntry.Message);
                    break;
                case SpecMetrix.Interfaces.LogLevel.Trace:
                    logger.Verbose(logEntry.Message);
                    break;
                default:
                    logger.Information(logEntry.Message);
                    break;
            }
        }

        /// <summary>
        /// Renders the message template.
        /// </summary>
        private string RenderMessageTemplate(string template, IDictionary<string, object> values, string originalMessage)
        {
            if (string.IsNullOrWhiteSpace(template))
                return originalMessage;

            foreach (var value in values)
            {
                template = template.Replace("{" + value.Key + "}", value.Value?.ToString() ?? string.Empty);
            }
            return template;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("LoggingService is running.");
            return Task.CompletedTask;
        }
    }
}
