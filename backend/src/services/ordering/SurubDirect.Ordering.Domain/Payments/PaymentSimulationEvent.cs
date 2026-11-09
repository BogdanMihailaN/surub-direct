namespace SurubDirect.Ordering.Domain;
public sealed record PaymentSimulationEvent(Guid OrderId, Guid OperationId, Guid AttemptId, long Version,
    string Action, string FromStatus, string ToStatus, string Reason, string Actor, DateTimeOffset CreatedAt)
{
    public Guid? PartialReturnId { get; init; }
    public decimal? RefundAmount { get; init; }
}
