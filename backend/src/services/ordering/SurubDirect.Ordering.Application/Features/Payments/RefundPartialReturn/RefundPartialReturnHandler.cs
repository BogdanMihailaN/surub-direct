using SurubDirect.BuildingBlocks;
namespace SurubDirect.Ordering.Application;
public sealed class RefundPartialReturnHandler(IPaymentSimulations payments)
{
    public Task Handle(Guid orderId, Guid requestId, Guid operationId, long expectedVersion, string? reason, string actor, CancellationToken ct)
    {
        if (requestId == Guid.Empty || operationId == Guid.Empty || expectedVersion < 0 || string.IsNullOrWhiteSpace(reason) || reason.Trim().Length is < 3 or > 500)
            throw new BusinessException("Confirmă operația și introdu un motiv de 3–500 caractere.");
        return payments.RefundReturn(orderId, requestId, operationId, expectedVersion, reason.Trim(), actor, ct);
    }
}
