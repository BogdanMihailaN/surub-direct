namespace SurubDirect.Ordering.Contracts;
public sealed record RequestMessageReplay(Guid RequestId, string? Reason);
