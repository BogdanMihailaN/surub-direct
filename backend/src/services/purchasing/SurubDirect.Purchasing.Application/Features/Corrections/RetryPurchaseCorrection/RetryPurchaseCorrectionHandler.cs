namespace SurubDirect.Purchasing.Application;

public sealed class RetryPurchaseCorrectionHandler(IPurchaseCorrectionRepository repository, IInventoryCorrections inventory)
{
    public async Task Handle(Guid orderId, Guid correctionId, CancellationToken ct)
    {
        var c = await repository.Get(orderId, correctionId, ct); if (c.AppliedAt is not null) return;
        await inventory.Apply(c.Id, c.ReceiptId, c.VariantId, c.Quantity, c.Reference, c.Reason, ct);
        await repository.Complete(orderId, c.Id, ct);
    }
}
