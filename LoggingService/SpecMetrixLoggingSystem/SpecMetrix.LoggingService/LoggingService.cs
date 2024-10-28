using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SpecMetrix.Interfaces;

public class LoggingService : BackgroundService, ILoggingService
{
    private readonly ConcurrentQueue<ILogEntry> _logQueue = new ConcurrentQueue<ILogEntry>();
    private readonly IDataService _dataService;
    private readonly TimeSpan _flushInterval;
    private readonly ILogger<LoggingService> _logger;

    // Deduplication wait interval
    private readonly int _eventWaitInterval;
    private readonly ConcurrentDictionary<string, (ILogEntry logEntry, DateTime lastLoggedTime)> _logCache = new ConcurrentDictionary<string, (ILogEntry, DateTime)>();

    /// <summary>
    /// DI Logging Service to receive and deduplicate logs then write to MongoDB in batched process
    /// </summary>
    /// <param name="dataService"></param>
    /// <param name="logger"></param>
    /// <param name="configuration"></param>
    public LoggingService(IDataService dataService, ILogger<LoggingService> logger, IConfiguration configuration)
    {
        _dataService = dataService;
        _logger = logger;

        // Load the EventWaitInterval and FlushInterval from the configuration under Config.Logging
        _eventWaitInterval = configuration.GetValue<int>("Config:Logging:EventWaitInterval", 5000); // Default to 5000 ms if not set
        int flushIntervalMs = configuration.GetValue<int>("Config:Logging:FlushInterval", 1000);    // Default to 1000 ms (1 second) if not set
        _flushInterval = TimeSpan.FromMilliseconds(flushIntervalMs);
    }

    /// <summary>
    /// This will put logs into queue and deduplicate rapid log entries.
    /// This should be accessed from an API Entry point
    /// </summary>
    /// <param name="logEntry"></param>
    public void EnqueueLog(ILogEntry logEntry)
    {
        // Use Code, Process, Category, and Source as the deduplication key
        var logKey = $"{logEntry.Code}-{logEntry.Process}-{logEntry.Category}-{logEntry.Source}";

        if (_logCache.TryGetValue(logKey, out var cachedLogEntry))
        {
            var timeSinceLastLog = DateTime.UtcNow - cachedLogEntry.lastLoggedTime;

            if (timeSinceLastLog.TotalMilliseconds < _eventWaitInterval)
            {
                // Increment occurrences of the cached log entry if it's within the wait interval
                cachedLogEntry.logEntry.Occurrences++;
                _logCache[logKey] = (cachedLogEntry.logEntry, cachedLogEntry.lastLoggedTime);
                return; // Do not enqueue the log yet
            }
        }

        // Update the cache and enqueue the log entry if it's outside the wait interval
        _logCache[logKey] = (logEntry, DateTime.UtcNow);
        _logQueue.Enqueue(logEntry);
    }

    // The worker will run in the background, processing log entries every 'flushInterval'
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_flushInterval, stoppingToken); // Wait for the configured flush interval
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
