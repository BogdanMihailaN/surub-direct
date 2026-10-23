using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Application;
public interface IExpiredCartCleanup
{
    Task<ExpiredCartCleanupResult> Run(int graceDays, int batchSize, bool dryRun, CancellationToken ct);
}
