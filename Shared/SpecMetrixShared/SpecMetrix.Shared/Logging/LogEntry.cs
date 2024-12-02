using SpecMetrix.Interfaces;

namespace SpecMetrix.Shared.Logging;

/// <summary>
/// Log Entry for SpecMetrix systems
/// </summary>
public class LogEntry : ILogEntry
{
    public Guid LogId { get; set; }
    public string Namespace { get; set; }
    public DateTime Timestamp { get; set; }
    public string MachineName { get; set; }
    public int Code { get; set; }
    public string Process { get; set; }
    public LogLevel Level { get; set; }
    public string? ClassMethod { get; set; }
    public string Message { get; set; }
    public ulong Occurrences { get; set; }
    public IDictionary<string, string>? Metadata { get; set; }
    public string? Source { get; set; }
    public LogCategory? Category { get; set; }
    public string? ExceptionMessage { get; set; }
    public string? StackTrace { get; set; }

    /// <summary>
    /// Message Template for Serilog based logging
    /// </summary>
    public string MessageTemplate { get; set; } // Holds the message template, e.g., "User {UserId} logged in from {Location} at {LoginTime}"
    public IDictionary<string, object> TemplateValues { get; set; } // Holds the values for placeholders
    public string? DeviceName { get; set; }

    public string RenderedMessage { get; set; }

    public LogEntry()
    {

    }
}
