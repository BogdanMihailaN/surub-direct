namespace SurubDirect.Ordering.Contracts;
public sealed record ShipmentChangeDto(Guid OperationId, long Version, string FromStatus, string ToStatus,
    string Reason, string Actor, DateTimeOffset CreatedAt);
