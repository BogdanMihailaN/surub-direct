using SurubDirect.BuildingBlocks;
namespace SurubDirect.Ordering.Application;
public sealed class ChangeShipmentHandler(IShipments orders)
{
    public Task Handle(Guid id, Guid operationId, long expectedVersion, string? target, string? reason, string actor, CancellationToken ct)
    {
        if (operationId == Guid.Empty || expectedVersion < 0 || target is not ("Ready" or "Shipped" or "Delivered")
            || string.IsNullOrWhiteSpace(reason) || reason.Trim().Length is < 3 or > 500)
            throw new BusinessException("Selectează etapa și introdu un motiv de 3–500 caractere.");
        return orders.Change(id, operationId, expectedVersion, target, reason.Trim(), actor, ct);
    }
}
