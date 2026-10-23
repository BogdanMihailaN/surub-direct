namespace SurubDirect.Inventory.Contracts;
public sealed record OutboxRetentionResult(int Candidates, int Removed, bool DryRun);
