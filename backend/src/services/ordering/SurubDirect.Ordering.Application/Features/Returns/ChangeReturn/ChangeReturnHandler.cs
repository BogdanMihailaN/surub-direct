using SurubDirect.BuildingBlocks;
namespace SurubDirect.Ordering.Application;
public sealed class ChangeReturnHandler(IOrderReturns orders)
{
    public Task Handle(Guid id, Guid operationId, long expectedVersion, string? target, string? reason, string actor, bool restockConfirmed, CancellationToken ct)
    {
        if (operationId == Guid.Empty || expectedVersion < 0 || target is not ("Requested" or "Approved" or "Rejected" or "Restoring")
            || string.IsNullOrWhiteSpace(reason) || reason.Trim().Length is < 3 or > 500)
            throw new BusinessException("Selectează etapa și introdu un motiv de 3–500 caractere.");
        if (target == "Restoring" && !restockConfirmed)
            throw new BusinessException("Confirmă primirea integrală și starea revandabilă a produselor.");
        return orders.Change(id, operationId, expectedVersion, target, reason.Trim(), actor, ct);
    }
}
