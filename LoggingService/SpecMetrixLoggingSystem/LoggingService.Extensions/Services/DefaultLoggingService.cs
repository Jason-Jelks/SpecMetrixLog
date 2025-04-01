using LoggingService.Extensions.Interfaces;
using SpecMetrix.Interfaces;

namespace LoggingService.Extensions.Services
{
    public class DefaultLoggingService : ILoggingService
    {
        private readonly ISerilogWrapper _serilog;

        public DefaultLoggingService(ISerilogWrapper serilog)
        {
            _serilog = serilog;
        }

        public void EnqueueLog(ILogEntry logEntry)
        {
            _serilog.Log(logEntry);
        }
    }
}
