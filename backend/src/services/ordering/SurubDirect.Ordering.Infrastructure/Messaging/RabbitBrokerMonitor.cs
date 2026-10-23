using SurubDirect.Messaging;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Infrastructure;
public sealed class RabbitBrokerMonitor(BrokerSettings settings, BrokerTopology topology) : IBrokerMonitor
{
    public async Task<BrokerStatusDto> Read(CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(8));
        try
        {
            await using var session = await RabbitSession.Open(settings, topology, timeout.Token, false);
            var ready = await session.ReadyCount(false, timeout.Token);
            var dead = await session.ReadyCount(true, timeout.Token);
            return new(true, true, ready, dead, DateTimeOffset.UtcNow);
        }
        catch (Exception)when (!ct.IsCancellationRequested)
        {
            return new(true, false, null, null, DateTimeOffset.UtcNow);
        }
    }
}
