using SpecMetrix.Interfaces;
using Serilog;

public class LoggingService : BackgroundService, ILoggingService
{
    private readonly ILogger<LoggingService> _logger;

    public LoggingService(ILogger<LoggingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Instead of handling deduplication manually, log entries are passed directly to Serilog
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
        
        // Log based on log level
        switch (logEntry.Level)
        {
            case SpecMetrix.Interfaces.LogLevel.Critical:
                if (string.IsNullOrEmpty(logEntry.MessageTemplate))
                    logger.Fatal(logEntry.Message);
                else
                    logger.Fatal(logEntry.MessageTemplate, logEntry.TemplateValues);
                break;
            case SpecMetrix.Interfaces.LogLevel.Error:
                if (string.IsNullOrEmpty(logEntry.MessageTemplate))
                    logger.Error(logEntry.Message);
                else
                    logger.Error(logEntry.MessageTemplate, logEntry.TemplateValues);
                break;
            case SpecMetrix.Interfaces.LogLevel.Warning:
                if (string.IsNullOrEmpty(logEntry.MessageTemplate))
                    logger.Warning(logEntry.Message);
                else
                    logger.Warning(logEntry.MessageTemplate, logEntry.TemplateValues);
                break;
            case SpecMetrix.Interfaces.LogLevel.Debug:
                if (string.IsNullOrEmpty(logEntry.MessageTemplate))
                    logger.Debug(logEntry.Message);
                else
                    logger.Debug(logEntry.MessageTemplate, logEntry.TemplateValues);
                break;
            case SpecMetrix.Interfaces.LogLevel.Trace:
                if (string.IsNullOrEmpty(logEntry.MessageTemplate))
                    logger.Verbose(logEntry.Message);
                else
                    logger.Verbose(logEntry.MessageTemplate, logEntry.TemplateValues);
                break;
            default:
                if (string.IsNullOrEmpty(logEntry.MessageTemplate))
                    logger.Information(logEntry.Message);
                else
                    logger.Information(logEntry.MessageTemplate, logEntry.TemplateValues);
                break;
        }
    }


    // Helper to render template message
    private string RenderMessageTemplate(string template, IDictionary<string, object> values, string orignalMessage)
    {
        if (string.IsNullOrWhiteSpace(template))
            return orignalMessage;

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
