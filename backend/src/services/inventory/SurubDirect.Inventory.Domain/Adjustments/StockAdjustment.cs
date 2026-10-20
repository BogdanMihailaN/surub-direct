using SurubDirect.BuildingBlocks;
namespace SurubDirect.Inventory.Domain;
public sealed class StockAdjustment
{
    private StockAdjustment() { }
    public Guid Id { get; private set; }
    public Guid VariantId { get; private set; }
    public int Quantity { get; private set; }
    public long ExpectedOnHand { get; private set; }
    public long ExpectedReserved { get; private set; }
    public string Reference { get; private set; } = "";
    public string Reason { get; private set; } = "";
    public string Actor { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public static StockAdjustment Create(Guid id, Guid variant, int quantity, long onHand, long reserved, string reference, string reason, string actor)
    {
        if (id == Guid.Empty || variant == Guid.Empty) throw new BusinessException("Lipsește identificatorul operației sau SKU-ului.");
        if (quantity == 0 || quantity < -10000000 || quantity > 10000000) throw new BusinessException("Ajustarea trebuie să fie între -10.000.000 și 10.000.000 bucăți, diferită de zero.");
        if (reserved < 0 || onHand < reserved) throw new BusinessException("Sold așteptat invalid.");
        return new() { Id = id, VariantId = variant, Quantity = quantity, ExpectedOnHand = onHand, ExpectedReserved = reserved,
            Reference = Guard.Text(reference, "Document", 100), Reason = Guard.Text(reason, "Motiv", 500), Actor = Guard.Text(actor, "Operator") };
    }
    public void EnsureSame(StockAdjustment other)
    {
        if (VariantId != other.VariantId || Quantity != other.Quantity || Reference != other.Reference || Reason != other.Reason || ExpectedOnHand != other.ExpectedOnHand || ExpectedReserved != other.ExpectedReserved)
            throw new BusinessException("Cheia a fost folosită pentru o altă ajustare.", 409);
    }
    public long Apply(long onHand, long reserved)
    {
        if (onHand != ExpectedOnHand || reserved != ExpectedReserved) throw new BusinessException("Stocul s-a modificat. Reîncarcă înainte de ajustare.", 409);
        if (Quantity > 0 && onHand > long.MaxValue - Quantity) throw new BusinessException("Limita soldului ar fi depășită.", 409);
        var result = onHand + Quantity;
        if (result < reserved) throw new BusinessException("Ajustarea ar reduce stocul sub cantitatea rezervată sau sub zero.", 409);
        return result;
    }
}
