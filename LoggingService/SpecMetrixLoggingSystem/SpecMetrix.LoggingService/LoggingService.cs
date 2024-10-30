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
            .Information(logEntry.Message);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LoggingService is running.");
        return Task.CompletedTask;
    }
}
