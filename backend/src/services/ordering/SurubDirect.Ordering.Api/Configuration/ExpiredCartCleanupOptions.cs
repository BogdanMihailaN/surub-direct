using System.ComponentModel.DataAnnotations;

namespace SurubDirect.Ordering.Api.Configuration;
public sealed class ExpiredCartCleanupOptions
{
    public bool Enabled { get; set; } = true;
    public bool DryRun { get; set; } = true;

    [Range(1, 3650)]
    public int GraceDays { get; set; } = 7;

    [Range(1, 1000)]
    public int BatchSize { get; set; } = 100;

    [Range(60, 86400)]
    public int IntervalSeconds { get; set; } = 3600;
}
