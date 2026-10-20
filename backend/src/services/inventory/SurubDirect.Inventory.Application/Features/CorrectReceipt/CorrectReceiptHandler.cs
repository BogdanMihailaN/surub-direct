using SurubDirect.Inventory.Domain;
namespace SurubDirect.Inventory.Application;

public sealed class CorrectReceiptHandler(IReceiptCorrections repository)
{
    public Task<bool> Handle(CorrectReceipt c, string actor, CancellationToken ct) => repository.Apply(ReceiptCorrection.Create(c.RequestId, c.ReceiptId, c.VariantId, c.Quantity, c.Reference, c.Reason, actor), ct);
}
