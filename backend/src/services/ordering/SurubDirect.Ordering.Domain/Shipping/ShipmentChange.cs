namespace SurubDirect.Ordering.Domain;

public sealed record ShipmentChange(Guid OrderId, Guid OperationId, long Version, string FromStatus,
    string ToStatus, string Reason, string Actor, DateTimeOffset CreatedAt);
