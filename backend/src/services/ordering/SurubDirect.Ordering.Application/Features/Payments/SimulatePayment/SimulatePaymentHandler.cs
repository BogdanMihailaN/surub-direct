using SurubDirect.BuildingBlocks;
namespace SurubDirect.Ordering.Application;
public sealed class SimulatePaymentHandler(IPaymentSimulations simulations)
{
    public Task Handle(Guid id, Guid operationId, long expectedVersion, string? action, string? reason, string actor, CancellationToken ct)
    {
        if (operationId == Guid.Empty || expectedVersion < 0 || action is not ("Start" or "Succeed" or "Fail" or "Refund")
            || string.IsNullOrWhiteSpace(reason) || reason.Trim().Length is < 3 or > 500)
            throw new BusinessException("Alege operația simulată și un motiv de 3–500 caractere.");
        return simulations.Apply(id, operationId, expectedVersion, action, reason.Trim(), actor, ct);
    }
}
