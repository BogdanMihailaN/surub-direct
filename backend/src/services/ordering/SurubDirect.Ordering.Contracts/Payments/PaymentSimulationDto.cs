namespace SurubDirect.Ordering.Contracts;
public sealed record PaymentSimulationDto(string Status, long Version, Guid? AttemptId, decimal Amount, string Currency,
    IReadOnlyList<PaymentSimulationEventDto> History);
