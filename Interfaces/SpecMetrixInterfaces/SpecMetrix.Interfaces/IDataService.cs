namespace SpecMetrix.Interfaces
{
    public interface IDataService
    {
        /// <summary>
        /// Writes a single log entry to the database.
        /// </summary>
        Task WriteLogAsync(ILogEntry logEntry);


        /// <summary>
        /// Batch Write log entries to the database.
        /// </summary>
        Task WriteLogsAsync(IEnumerable<ILogEntry> logEntries);

        /// <summary>
        /// Retrieves logs with optional query filters.
        /// </summary>
        Task<IEnumerable<ILogEntry>> ReadLogsAsync(LogQueryOptions queryOptions);
    }

    /// <summary>
    /// Options for querying logs from the database.
    /// </summary>
    public class LogQueryOptions
    {
        public DateTime? StartDate { get; set; } // Filter logs from a start date
        public DateTime? EndDate { get; set; } // Filter logs up to an end date
        public LogLevel? LogLevel { get; set; } // Filter by log level
        public string? Process { get; set; } // Filter by process name (e.g., "Database", "Core")
        public LogCategory? Category { get; set; } // 
        public string? Source { get; set; }
        public int? Code { get; set; }
        public string? ClassMethod { get; set; }
        public int? HowManyLogsToGet { get; set; }
    }
}
