
namespace SpecMetrix.Interfaces;

public interface ILogEntry
{
    /// <summary>
    /// Unique identifier for the log entry
    /// </summary>
    Guid LogId { get; set; }

    /// <summary>
    /// DateTime the event message occurred
    /// </summary>
    DateTime Timestamp { get; set; }

    /// <summary>
    /// Numeric code given for the event
    /// </summary>
    int Code { get; set; }

    /// <summary>
    /// High level process name given where this event originated (e.g. Core, Database, Spectrometer, IO)
    /// </summary>
    string Process { get; set; } 

    /// <summary>
    /// Logging Level { debug, info, warn, error, fatal }
    /// </summary>
    LogLevel Level { get; set; }

    /// <summary>
    /// (Optional) Class & Method where the event took place. Used for developer information to isolate origination of the event
    /// </summary>
    string? ClassMethod { get; set; }

    /// <summary>
    /// Event message to the user
    /// </summary>
    string Message { get; set; }

    /// <summary>
    /// Number of repetitive occurrence that this event occurred without stopping
    /// </summary>
    ulong Occurrences { get; set; }

    /// <summary>
    /// Optional contextual data (e.g., user ID, session ID)
    /// </summary>
    IDictionary<string, string>? Context { get; set; }

    /// <summary>
    /// Optional source (e.g., application or server name)
    /// </summary>
    string? Source { get; set; }

    /// <summary>
    /// Optional category to group similar log entries
    /// </summary>
    LogCategory? Category { get; set; }

    /// <summary>
    /// Exception Message
    /// </summary>
    ExceptionDetails? Exception { get; set; }
}

public class ExceptionDetails
{
    public string? Message { get; set; }
    public string? StackTrace { get; set; }
}

public enum LogCategory
{
    Authentication,
    Database,
    Spectrometer,
    Integration,
    Communication,
    UI,
    PLC
}