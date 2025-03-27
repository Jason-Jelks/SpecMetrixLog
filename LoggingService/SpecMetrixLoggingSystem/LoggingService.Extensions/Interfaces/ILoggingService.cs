namespace LoggingService.Extensions.Interfaces
{
    using SpecMetrix.Interfaces;

    public interface ILoggingService
    {
        void EnqueueLog(ILogEntry logEntry);
    }
}
