using SurubDirect.Purchasing.Domain;
namespace SurubDirect.Purchasing.Application;

public interface IPurchaseCorrectionRepository
{
    Task<PurchaseReceiptCorrection> Reserve(Guid orderId, CorrectPurchaseReceipt command, string actor, CancellationToken ct);
    Task<PurchaseReceiptCorrection> Get(Guid orderId, Guid correctionId, CancellationToken ct);
    Task Complete(Guid orderId, Guid correctionId, CancellationToken ct);
}
