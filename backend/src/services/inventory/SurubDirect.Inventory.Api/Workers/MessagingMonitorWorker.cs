using Microsoft.Extensions.Options;
using SurubDirect.Inventory.Application;
using SurubDirect.ServiceDefaults.Monitoring;

namespace SurubDirect.Inventory.Api.Workers;
public sealed class MessagingMonitorWorker(IServiceScopeFactory scopes, IOptions<OperationalMonitoringOptions> options, TimeProvider clock, ILogger<MessagingMonitorWorker> logger) : OperationalMonitorWorker(options, clock, logger)
{
    public override Task CheckOnce(CancellationToken ct) => Probe("inventory.outbox-unavailable", async token =>
    {
        await using var scope = scopes.CreateAsyncScope();
        var outbox = await scope.ServiceProvider.GetRequiredService<IOutboxMonitor>().Read(token);
        var now = Now;
        var ageUnknown = outbox.Pending > 0 && (outbox.OldestPendingAt is null || outbox.OldestPendingAt > now);
        Alerts.Observe("inventory.outbox-age-unknown", ageUnknown, now);
        Alerts.Observe("inventory.outbox-delayed", ageUnknown ? null : outbox.Pending > 0 && now - outbox.OldestPendingAt >= TimeSpan.FromMinutes(5), now);
    }, ct);
}
