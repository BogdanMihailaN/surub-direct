namespace SurubDirect.Ordering.Contracts;
public sealed record OrderProcessingChangeDto(Guid OperationId, long Version, string FromStatus, string ToStatus,
    string Reason, string Actor, DateTimeOffset CreatedAt);
