using SurubDirect.Ordering.Domain;
namespace SurubDirect.Ordering.Application;
public sealed class ProcessPartialReturnReceiptHandler(IPartialReturnReceipts receipts, IPartialReturnInventory inventory, TimeProvider clock)
{
    public Task<int> Handle(CancellationToken ct) => receipts.Process(Reconcile, ct);
    private async Task Reconcile(PartialReturnRequest request, CancellationToken ct)
    {
        try { request.ConfirmReceipt(await inventory.Receive(request, ct)); }
        catch (Exception) when (!ct.IsCancellationRequested) { request.ReceiptFailed(clock.GetUtcNow()); }
    }
}
