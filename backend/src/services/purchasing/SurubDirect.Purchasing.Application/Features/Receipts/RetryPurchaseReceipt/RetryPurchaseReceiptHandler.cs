namespace SurubDirect.Purchasing.Application;
public sealed class RetryPurchaseReceiptHandler(IPurchaseReceiptRepository repository, IInventoryReceipts inventory)
{
    public async Task Handle(Guid orderId, Guid receiptId, CancellationToken ct)
    {
        var receipt = await repository.Get(orderId, receiptId, ct);
        if (receipt.AppliedAt is not null) return;
        await inventory.Receive(receipt.Id, receipt.VariantId, receipt.Quantity, receipt.Reference, ct);
        await repository.Complete(orderId, receiptId, ct);
    }
}
