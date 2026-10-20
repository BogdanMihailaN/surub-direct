namespace SurubDirect.Purchasing.Application;

public sealed class CorrectPurchaseReceiptHandler(IPurchaseCorrectionRepository repository, IInventoryCorrections inventory)
{
    public async Task Handle(Guid orderId, CorrectPurchaseReceipt command, string actor, CancellationToken ct)
    {
        var c = await repository.Reserve(orderId, command, actor, ct); if (c.AppliedAt is not null) return;
        await inventory.Apply(c.Id, c.ReceiptId, c.VariantId, c.Quantity, c.Reference, c.Reason, ct);
        await repository.Complete(orderId, c.Id, ct);
    }
}
