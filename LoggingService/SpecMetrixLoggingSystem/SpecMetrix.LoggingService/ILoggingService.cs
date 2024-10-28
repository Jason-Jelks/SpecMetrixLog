using SpecMetrix.Interfaces;

public interface ILoggingService
{
    void EnqueueLog(ILogEntry logEntry);
}
