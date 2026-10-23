using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Application;
public interface IFailedMessageRetention
{
    Task<FailedMessageRetentionResult> Run(int retentionDays, int batchSize, bool dryRun, CancellationToken ct);
}
