using SurubDirect.Inventory.Contracts;

namespace SurubDirect.Inventory.Application;
public interface IOutboxMonitor
{
    Task<OutboxStatusDto> Read(CancellationToken ct);
}
