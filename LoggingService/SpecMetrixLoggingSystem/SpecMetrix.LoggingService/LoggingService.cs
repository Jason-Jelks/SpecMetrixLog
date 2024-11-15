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
        logEntry.Message = RenderMessageTemplate(logEntry.MessageTemplate, logEntry.TemplateValues);

        // Create the logger context with the dynamic namespace and additional context properties
        var logger = Log.ForContext(new NamespaceEnricher(logEntry.Namespace ?? string.Empty))
                        .ForContext("Code", logEntry.Code)
                        .ForContext("Process", logEntry.Process)
                        .ForContext("Category", logEntry.Category)
                        .ForContext("Source", logEntry.Source)
                        .ForContext("DeviceName", logEntry.DeviceName)
                        .ForContext("MachineName", logEntry.MachineName ?? string.Empty)
                        .ForContext("ClassMethod", logEntry.ClassMethod);
        
        // Log based on log level
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


    // Helper to render template message
    private string RenderMessageTemplate(string template, IDictionary<string, object> values)
    {
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
