using SurubDirect.Inventory.Contracts;

namespace SurubDirect.Inventory.Application;
public interface IOutboxRetention
{
    Task<OutboxRetentionResult> Run(int retentionDays, int batchSize, bool dryRun, CancellationToken ct);
}
