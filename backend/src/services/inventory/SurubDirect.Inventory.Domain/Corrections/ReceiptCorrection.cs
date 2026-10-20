using SurubDirect.BuildingBlocks;
namespace SurubDirect.Inventory.Domain;

public sealed class ReceiptCorrection
{
    private ReceiptCorrection() { }
    public Guid Id { get; private set; }
    public Guid ReceiptId { get; private set; }
    public Guid VariantId { get; private set; }
    public int Quantity { get; private set; }
    public string Reference { get; private set; } = "";
    public string Reason { get; private set; } = "";
    public string Actor { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public static ReceiptCorrection Create(Guid id, Guid receiptId, Guid variantId, int quantity, string reference, string reason, string actor)
    {
        if (id == Guid.Empty || receiptId == Guid.Empty || variantId == Guid.Empty) throw new BusinessException("Identificator invalid.");
        if (quantity < 1 || quantity > 10000000) throw new BusinessException("Corecția trebuie să retragă între 1 și 10.000.000 bucăți.");
        return new() { Id = id, ReceiptId = receiptId, VariantId = variantId, Quantity = quantity, Reference = Guard.Text(reference, "Document", 100).ToUpperInvariant(), Reason = Guard.Text(reason, "Motiv", 500), Actor = Guard.Text(actor, "Operator") };
    }
    public void EnsureSame(ReceiptCorrection c)
    {
        if (ReceiptId != c.ReceiptId || VariantId != c.VariantId || Quantity != c.Quantity || Reference != c.Reference || Reason != c.Reason) throw new BusinessException("Cheia corecției a fost folosită cu alte date.", 409);
    }
}
