using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LoggingService.Health
{
    public sealed class MongoWriteHealthCheck : IHealthCheck
    {
        private readonly MongoLogService _mongo;

        public MongoWriteHealthCheck(MongoLogService mongo)
        {
            _mongo = mongo;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            var (ok, error) = await _mongo.VerifyWriteAsync(cts.Token);
            if (ok)
                return HealthCheckResult.Healthy("MongoDB write path is healthy.");

            return new HealthCheckResult(
                status: HealthStatus.Unhealthy,
                description: "MongoDB write path failed.",
                exception: null,
                data: new Dictionary<string, object?> { { "error", error ?? "unknown" } });
        }
    }
}
