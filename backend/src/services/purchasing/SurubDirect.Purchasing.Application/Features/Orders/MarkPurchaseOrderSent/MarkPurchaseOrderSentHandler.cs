using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Domain;
namespace SurubDirect.Purchasing.Application;
public sealed class MarkPurchaseOrderSentHandler(IPurchaseOrderRepository repository, ICatalogVariants catalog)
{
    public Task Handle(Guid id, MarkPurchaseOrderSent c, CancellationToken ct) => repository.Change(id, c.Version, async (order, supplier) =>
    {
        order.EnsureCanSend(supplier);
        var variants = await Task.WhenAll(order.Lines.Select(line => catalog.Find(line.VariantId, ct)));
        if (variants.Any(v => v is null)) throw new BusinessException("Un produs a fost arhivat sau nu mai există. Verifică articolele înainte de trimitere.", 409);
        order.MarkSent(supplier);
    }, ct);
}
