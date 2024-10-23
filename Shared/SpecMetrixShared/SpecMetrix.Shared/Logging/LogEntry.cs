using SpecMetrix.Interfaces;

namespace SpecMetrix.Shared.Logging;

/// <summary>
/// Log Entry for SpecMetrix systems
/// </summary>
public class LogEntry : ILogEntry
{
    public Guid LogId { get; set; }
    public DateTime Timestamp { get; set; }
    public int Code { get; set; }
    public string Process { get; set; }
    public LogLevel Level { get; set; }
    public string? ClassMethod { get; set; }
    public string Message { get; set; }
    public ulong Occurrences { get; set; }
    public IDictionary<string, string>? Context { get; set; }
    public string? Source { get; set; }
    public LogCategory? Category { get; set; }
    public ExceptionDetails? Exception { get; set; }

    public LogEntry()
    {

    }
}
