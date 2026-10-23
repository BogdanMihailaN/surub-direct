using System.ComponentModel.DataAnnotations;

namespace SurubDirect.ServiceDefaults.Monitoring;
public sealed class OperationalMonitoringOptions
{
    public bool Enabled { get; set; } = true;

    [Range(5, 3600)]
    public int IntervalSeconds { get; set; } = 60;

    [Range(1, 60)]
    public int ProbeTimeoutSeconds { get; set; } = 10;

    [Range(60, 86400)]
    public int ReminderSeconds { get; set; } = 1800;
}
