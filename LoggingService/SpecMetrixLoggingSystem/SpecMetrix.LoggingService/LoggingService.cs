using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        // Log to Serilog with structured data
        Log.ForContext("Code", logEntry.Code)
            .ForContext("Process", logEntry.Process)
            .ForContext("Category", logEntry.Category)
            .ForContext("Source", logEntry.Source)
            .ForContext("ClassMethod", logEntry.ClassMethod ?? "None");

        switch (logEntry.Level)
        {
            case SpecMetrix.Interfaces.LogLevel.Fatal:
                Log.Fatal(logEntry.Message);
                break;
            case SpecMetrix.Interfaces.LogLevel.Error:
                Log.Error(logEntry.Message);
                break;
            case SpecMetrix.Interfaces.LogLevel.Warning:
                Log.Warning(logEntry.Message);
                break;
            case SpecMetrix.Interfaces.LogLevel.Debug:
            case SpecMetrix.Interfaces.LogLevel.Verbose:
                Log.Debug(logEntry.Message);
                break;
            default:
                Log.Information(logEntry.Message);  // Default to Information if no level matches
                break;
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LoggingService is running.");
        return Task.CompletedTask;
    }
}
