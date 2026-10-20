using SurubDirect.BuildingBlocks;
namespace SurubDirect.Purchasing.Domain;

public sealed class PurchaseReceiptCorrection
{
    private PurchaseReceiptCorrection() { }
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ReceiptId { get; private set; }
    public Guid LineId { get; private set; }
    public Guid VariantId { get; private set; }
    public int Quantity { get; private set; }
    public string Reference { get; private set; } = "";
    public string Reason { get; private set; } = "";
    public string Actor { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? AppliedAt { get; private set; }
    public static PurchaseReceiptCorrection Create(Guid id, PurchaseReceipt receipt, int quantity, string reference, string reason, string actor)
    {
        if (id == Guid.Empty || quantity < 1 || quantity > 10000000) throw new BusinessException("Cheie sau cantitate de corecție invalidă.");
        return new() { Id = id, OrderId = receipt.OrderId, ReceiptId = receipt.Id, LineId = receipt.LineId, VariantId = receipt.VariantId, Quantity = quantity, Reference = PurchaseReceipt.NormalizeReference(reference), Reason = Guard.Text(reason, "Motiv", 500), Actor = Guard.Text(actor, "Operator") };
    }
    public void EnsureSame(Guid order, Guid receipt, int quantity, string reference, string reason)
    {
        if (OrderId != order || ReceiptId != receipt || Quantity != quantity || Reference != PurchaseReceipt.NormalizeReference(reference) || Reason != Guard.Text(reason, "Motiv", 500)) throw new BusinessException("Cheia corecției a fost folosită cu alte date.", 409);
    }
    public void MarkApplied() { AppliedAt ??= DateTimeOffset.UtcNow; }
}
