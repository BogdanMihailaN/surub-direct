using System.ComponentModel.DataAnnotations;

namespace SurubDirect.Inventory.Api.Configuration;
public sealed class OutboxRetentionOptions
{
    public bool Enabled { get; set; } = true;
    public bool DryRun { get; set; } = true;

    [Range(30, 3650)]
    public int RetentionDays { get; set; } = 30;

    [Range(1, 1000)]
    public int BatchSize { get; set; } = 100;

    [Range(60, 86400)]
    public int IntervalSeconds { get; set; } = 3600;
}
