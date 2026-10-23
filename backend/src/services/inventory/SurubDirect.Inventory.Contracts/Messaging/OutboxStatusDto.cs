namespace SurubDirect.Inventory.Contracts;
public sealed record OutboxStatusDto(int Pending, DateTimeOffset? OldestPendingAt, DateTimeOffset? LastPublishedAt);
