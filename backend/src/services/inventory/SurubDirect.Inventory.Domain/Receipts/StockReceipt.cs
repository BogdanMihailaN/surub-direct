using SurubDirect.BuildingBlocks;

namespace SurubDirect.Inventory.Domain;
public sealed class StockReceipt
{
    private StockReceipt()
    {
    }

    public Guid Id { get; private set; }
    public Guid VariantId { get; private set; }
    public int Quantity { get; private set; }
    public string Reference { get; private set; } = "";
    public string Actor { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public static StockReceipt Create(Guid id, Guid variant, int quantity, string reference, string actor)
    {
        Guard.Positive(quantity, "Cantitate");
        if (quantity > 10000000)
            throw new BusinessException("Cantitatea maximă per recepție este 10.000.000.");
        if (id == Guid.Empty)
            throw new BusinessException("Lipsește cheia de idempotență.");
        return new()
        {
            Id = id,
            VariantId = variant,
            Quantity = quantity,
            Reference = Guard.Text(reference, "Document recepție", 100),
            Actor = Guard.Text(actor, "Operator")
        };
    }
}
