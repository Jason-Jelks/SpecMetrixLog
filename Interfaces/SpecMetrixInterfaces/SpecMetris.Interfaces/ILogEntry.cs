
namespace SpecMetrix.Interfaces;

public interface ILogEntry
{
    /// <summary>
    /// DateTime the event message occurred
    /// </summary>
    DateTime Timestamp { get; set; }

    /// <summary>
    /// Numeric code given for the event
    /// </summary>
    int Code { get; set; }

    /// <summary>
    /// High level process name given where this event originated
    /// </summary>
    string Process { get; set; }  // e.g., Core, Database, Spectrometer, IO

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
}
