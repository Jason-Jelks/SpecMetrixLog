namespace LoggingService.Extensions.Interfaces
{
    using SpecMetrix.Interfaces;

    public interface ISerilogWrapper
    {
        void Log(ILogEntry logEntry);
    }
}
