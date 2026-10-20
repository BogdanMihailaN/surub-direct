namespace SurubDirect.Purchasing.Application;
public sealed class ReceivePurchaseOrderHandler(IPurchaseReceiptRepository repository, IInventoryReceipts inventory)
{
    public async Task Handle(Guid orderId, ReceivePurchaseOrder command, string actor, CancellationToken ct)
    {
        var receipt = await repository.Reserve(orderId, command, actor, ct);
        if (receipt.AppliedAt is not null) return;
        await inventory.Receive(receipt.Id, receipt.VariantId, receipt.Quantity, receipt.Reference, ct);
        await repository.Complete(orderId, receipt.Id, ct);
    }
}
