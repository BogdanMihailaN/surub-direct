using SurubDirect.Purchasing.Domain;
namespace SurubDirect.Purchasing.Application;
public interface IPurchaseReceiptRepository
{
    Task<PurchaseReceipt> Reserve(Guid orderId, ReceivePurchaseOrder command, string actor, CancellationToken ct);
    Task<PurchaseReceipt> Get(Guid orderId, Guid receiptId, CancellationToken ct);
    Task Complete(Guid orderId, Guid receiptId, CancellationToken ct);
}
