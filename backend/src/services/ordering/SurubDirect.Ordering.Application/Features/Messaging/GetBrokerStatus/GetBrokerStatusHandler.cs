using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Application;
public sealed class GetBrokerStatusHandler(IBrokerMonitor monitor)
{
    public Task<BrokerStatusDto> Handle(CancellationToken ct) => monitor.Read(ct);
}
