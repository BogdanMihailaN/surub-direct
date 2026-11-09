namespace SurubDirect.Ordering.Contracts;
public sealed record PaymentSimulationEventDto(Guid OperationId, Guid AttemptId, long Version, string Action,
    string FromStatus, string ToStatus, string Reason, string Actor, DateTimeOffset CreatedAt, Guid? PartialReturnId, decimal? RefundAmount);
