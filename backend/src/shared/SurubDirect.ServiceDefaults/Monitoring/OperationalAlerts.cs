using Microsoft.Extensions.Logging;

namespace SurubDirect.ServiceDefaults.Monitoring;
public sealed class OperationalAlerts(ILogger logger, TimeSpan reminder)
{
    private readonly Dictionary<string, DateTimeOffset> active = new();
    // A missing observation must never resolve an existing alert.
    public void Observe(string code, bool? isActive, DateTimeOffset now)
    {
        if (isActive is null)
            return;
        if (isActive.Value)
        {
            if (!active.TryGetValue(code, out var last))
            {
                active[code] = now;
                logger.LogWarning(new EventId(7100, "OperationalAlertRaised"), "Operational alert {AlertCode}: {AlertState} at {ObservedAt}", code, "Raised", now);
            }
            else if (now - last >= reminder)
            {
                active[code] = now;
                logger.LogWarning(new EventId(7101, "OperationalAlertReminder"), "Operational alert {AlertCode}: {AlertState} at {ObservedAt}", code, "Reminder", now);
            }
        }
        else if (active.Remove(code))
            logger.LogInformation(new EventId(7102, "OperationalAlertRecovered"), "Operational alert {AlertCode}: {AlertState} at {ObservedAt}", code, "Recovered", now);
    }
}
