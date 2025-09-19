// MongoWriteHealthCheck.cs
using LoggingService.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public sealed class MongoWriteHealthCheck : IHealthCheck
{
    private readonly IMongoWriteVerifier _verifier;

    public MongoWriteHealthCheck(IMongoWriteVerifier verifier)
    {
        _verifier = verifier;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(2));

        var (ok, error) = await _verifier.VerifyWriteAsync(cts.Token);
        if (ok)
            return HealthCheckResult.Healthy("MongoDB write path is healthy.");

        return new HealthCheckResult(
            status: HealthStatus.Unhealthy,
            description: "MongoDB write path failed.",
            exception: null,
            data: new Dictionary<string, object?> { { "error", string.IsNullOrWhiteSpace(error) ? "unknown" : error } });
    }
}
