using Microsoft.Extensions.Options;
using SurubDirect.Ordering.Application;
using SurubDirect.ServiceDefaults.Monitoring;

namespace SurubDirect.Ordering.Api.Workers;
public sealed class MessagingMonitorWorker(IServiceScopeFactory scopes, IOptions<OperationalMonitoringOptions> options, TimeProvider clock, ILogger<MessagingMonitorWorker> logger) : OperationalMonitorWorker(options, clock, logger)
{
    public override async Task CheckOnce(CancellationToken ct)
    {
        await Probe("ordering.broker-probe-unavailable", async token =>
        {
            await using var scope = scopes.CreateAsyncScope();
            var broker = await scope.ServiceProvider.GetRequiredService<IBrokerMonitor>().Read(token);
            Alerts.Observe("ordering.broker-unavailable", !broker.Available, Now);
            if (!broker.Available)
                return;
            Alerts.Observe("ordering.ready-backlog", broker.Ready is { } ready ? ready >= 100 : null, Now);
            Alerts.Observe("ordering.dead-letter", broker.DeadReady is { } dead ? dead > 0 : null, Now);
        }, ct);
        await Probe("ordering.registry-unavailable", async token =>
        {
            await using var scope = scopes.CreateAsyncScope();
            var summary = await scope.ServiceProvider.GetRequiredService<IMessageFailureMonitor>().Read(token);
            Alerts.Observe("ordering.failures-awaiting-review", summary.AwaitingReview > 0, Now);
            Alerts.Observe("ordering.replays-awaiting-publication", summary.PendingReplays > 0, Now);
        }, ct);
    }
}
