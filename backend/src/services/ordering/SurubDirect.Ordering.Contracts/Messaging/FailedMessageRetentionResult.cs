namespace SurubDirect.Ordering.Contracts;
public sealed record FailedMessageRetentionResult(int Candidates, int Removed, bool DryRun);
