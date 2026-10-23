using SurubDirect.Inventory.Contracts;

namespace SurubDirect.Inventory.Application;
public sealed class GetOutboxStatusHandler(IOutboxMonitor monitor)
{
    public Task<OutboxStatusDto> Handle(CancellationToken ct) => monitor.Read(ct);
}
