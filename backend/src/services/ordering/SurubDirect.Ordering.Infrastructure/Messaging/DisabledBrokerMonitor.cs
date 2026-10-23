using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Infrastructure;
public sealed class DisabledBrokerMonitor : IBrokerMonitor
{
    public Task<BrokerStatusDto> Read(CancellationToken ct) => Task.FromResult(new BrokerStatusDto(false, false, null, null, DateTimeOffset.UtcNow));
}
