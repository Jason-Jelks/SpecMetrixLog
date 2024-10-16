namespace SpecMetrix.Interfaces;

public interface IDataService
{
    Task WriteLogAsync(ILogEntry logEntry);         // Write a log entry to the database
    Task<List<ILogEntry>> GetLatestLogsAsync(int limit);  // Fetch the latest log entries
    Task RollOffExpiredLogsAsync();                // Remove logs older than a certain threshold (e.g., 7 days)
}
