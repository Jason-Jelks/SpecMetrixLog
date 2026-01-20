using LoggingService;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SpecMetrix.LoggingService.Services
{
    /// <summary>
    /// Ensures logging storage (MongoDB collections) exists at startup.
    /// </summary>
    public sealed class LoggingStorageInitializer : IHostedService
    {
        private readonly MongoLogService _mongo;
        private readonly ILogger<LoggingStorageInitializer> _logger;

        public LoggingStorageInitializer(
            MongoLogService mongo,
            ILogger<LoggingStorageInitializer> logger)
        {
            _mongo = mongo;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _mongo.EnsureDatabaseAndCollection();
                _mongo.EnsureHealthCollection();
                _logger.LogInformation("Logging storage initialized.");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Logging storage initialization failed.");
                throw;
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
