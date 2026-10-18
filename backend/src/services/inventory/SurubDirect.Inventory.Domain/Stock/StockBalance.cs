namespace SurubDirect.Inventory.Domain;
public sealed class StockBalance
{
    public Guid VariantId { get; set; }
    public long OnHand { get; set; }
    public long Reserved { get; set; }
}
