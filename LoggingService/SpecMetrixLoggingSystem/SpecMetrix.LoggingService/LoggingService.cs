using System.Collections.Concurrent;
using SpecMetrix.Interfaces;

public class LoggingService : BackgroundService
{
    private readonly ConcurrentQueue<ILogEntry> _logQueue = new ConcurrentQueue<ILogEntry>();
    private readonly IDataService _dataService;
    private readonly TimeSpan _flushInterval = TimeSpan.FromSeconds(1);
    private readonly ILogger<LoggingService> _logger;

    // Deduplication wait interval
    private readonly int _eventWaitInterval;
    private readonly ConcurrentDictionary<string, (ILogEntry logEntry, DateTime lastLoggedTime)> _logCache = new ConcurrentDictionary<string, (ILogEntry, DateTime)>();

    public LoggingService(IDataService dataService, ILogger<LoggingService> logger, IConfiguration configuration)
    {
        _dataService = dataService;
        _logger = logger;

        // Load the EventWaitInterval from the configuration under Config.Logging.EventWaitInterval
        _eventWaitInterval = configuration.GetValue<int>("Config:Logging:EventWaitInterval", 5000); // Default to 5000 ms if not set
    }

    // Enqueue new log events
    public void EnqueueLog(ILogEntry logEntry)
    {
        var logKey = $"{logEntry.Code}-{logEntry.Process}-{logEntry.Message}";

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
