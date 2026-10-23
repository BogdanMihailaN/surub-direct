namespace SurubDirect.Ordering.Contracts;
public sealed record BrokerStatusDto(bool Configured, bool Available, uint? Ready, uint? DeadReady, DateTimeOffset CheckedAt);
