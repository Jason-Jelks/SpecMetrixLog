using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpecMetrix.Interfaces
{
    /// <summary>
    /// Generic data service for log persistence.
    /// 
    /// IMPORTANT:
    /// This interface remains in SpecMetrix.Interfaces and must NOT depend on SpecMetrix.Shared,
    /// otherwise you will create a circular dependency (Shared.Logging already depends on Interfaces).
    /// 
    /// Consumers should inject IDataService<MongoLogEntry> in .NET 10 services.
    /// </summary>
    public interface IDataService<TLogEntry> where TLogEntry : ILogEntry
    {
        /// <summary>
        /// Writes a single log entry to the database.
        /// </summary>
        Task WriteLogAsync(TLogEntry logEntry);

        /// <summary>
        /// Batch write log entries to the database.
        /// </summary>
        Task WriteLogsAsync(IEnumerable<TLogEntry> logEntries);

        /// <summary>
        /// Retrieves logs with optional query filters.
        /// </summary>
        Task<IEnumerable<TLogEntry>> ReadLogsAsync(LogQueryOptions queryOptions);
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
