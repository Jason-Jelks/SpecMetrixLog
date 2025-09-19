using System.Threading;
using System.Threading.Tasks;

namespace LoggingService.Health
{
    public interface IMongoWriteVerifier
    {
        Task<(bool ok, string error)> VerifyWriteAsync(CancellationToken ct);
    }
}
