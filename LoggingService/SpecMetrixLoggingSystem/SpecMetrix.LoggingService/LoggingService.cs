using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpecMetrix.Interfaces;

public class LoggingService : BackgroundService
{
    private readonly ConcurrentQueue<ILogEntry> _logQueue = new ConcurrentQueue<ILogEntry>();
    private readonly IDataService _dataService;
    private readonly TimeSpan _flushInterval = TimeSpan.FromSeconds(1);
    private readonly ILogger<LoggingService> _logger;

    public LoggingService(IDataService dataService, ILogger<LoggingService> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    // Enqueue new log events
    public void EnqueueLog(ILogEntry logEntry)
    {
        _logQueue.Enqueue(logEntry);
    }

    // The worker will run in the background, processing log entries every second
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_flushInterval, stoppingToken); // Wait for 1 second
            await ProcessLogsAsync(); // Process the logs
        }
    }

    // Process the queued log events and write them to the database
    private async Task ProcessLogsAsync()
    {
        var logBatch = new List<ILogEntry>();

        while (_logQueue.TryDequeue(out var logEntry))
        {
            logBatch.Add(logEntry);
        }

        if (logBatch.Any())
        {
            try
            {
                await _dataService.WriteLogsAsync(logBatch); // Batch write to the database
                _logger.LogInformation("Successfully wrote {Count} logs to the database.", logBatch.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write logs to the database.");
            }
        }
    }
}
