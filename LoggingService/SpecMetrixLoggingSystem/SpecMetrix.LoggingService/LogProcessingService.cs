using SpecMetrix.Interfaces;
using LoggingService.Extensions.Interfaces;

namespace LoggingService
{
    public class LogProcessingService : BackgroundService, ILoggingService
    {
        private readonly ILogger<LogProcessingService> _logger;
        private readonly ISerilogWrapper _serilog;

        public LogProcessingService(
            ILogger<LogProcessingService> logger,
            MongoLogService mongoLogService,
            ISerilogWrapper serilogWrapper)
        {
            _logger = logger;
            _serilog = serilogWrapper;

            // Ensure MongoDB time-series collection is configured correctly
            mongoLogService.EnsureDatabaseAndCollection();
        }

        /// <summary>
        /// Enqueues a log entry using the injected Serilog wrapper.
        /// </summary>
        public void EnqueueLog(ILogEntry logEntry)
        {
            _serilog.Log(logEntry);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("LoggingService is running.");
            return Task.CompletedTask;
        }
    }
}
