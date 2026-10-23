namespace SurubDirect.Ordering.Contracts;
public sealed record ExpiredCartCleanupResult(int Candidates, int Removed, bool DryRun);
