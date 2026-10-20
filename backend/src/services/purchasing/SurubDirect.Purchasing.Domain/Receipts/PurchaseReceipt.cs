using SurubDirect.BuildingBlocks;
namespace SurubDirect.Purchasing.Domain;
public sealed class PurchaseReceipt
{
    private PurchaseReceipt() { }
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid LineId { get; private set; }
    public Guid VariantId { get; private set; }
    public int Quantity { get; private set; }
    public string Reference { get; private set; } = "";
    public string Actor { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? AppliedAt { get; private set; }
    public static PurchaseReceipt Create(Guid id, Guid orderId, PurchaseOrderLine line, int quantity, string reference, string actor)
    {
        if (id == Guid.Empty) throw new BusinessException("Lipsește cheia recepției.");
        if (quantity < 1 || quantity > 10000000) throw new BusinessException("Recepția trebuie să conțină între 1 și 10.000.000 bucăți.");
        return new() { Id = id, OrderId = orderId, LineId = line.Id, VariantId = line.VariantId, Quantity = quantity,
            Reference = NormalizeReference(reference), Actor = Guard.Text(actor, "Operator") };
    }
    public static string NormalizeReference(string reference) => Guard.Text(reference, "Document recepție", 100).ToUpperInvariant();
    public void EnsureSame(Guid orderId, Guid lineId, int quantity, string reference)
    {
        if (OrderId != orderId || LineId != lineId || Quantity != quantity || Reference != NormalizeReference(reference))
            throw new BusinessException("Cheia recepției a fost folosită cu alte date.", 409);
    }
    public void MarkApplied() { AppliedAt ??= DateTimeOffset.UtcNow; }
}
