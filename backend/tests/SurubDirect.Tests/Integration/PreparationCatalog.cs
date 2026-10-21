using SurubDirect.Ordering.Application;

namespace SurubDirect.Tests;
public sealed class PreparationCatalog : ICartCatalog
{
    public Guid VariantId { get; } = Guid.NewGuid();
    public Guid First { get; } = Guid.NewGuid();
    public Guid Second { get; } = Guid.NewGuid();
    public int Reads { get; private set; }
    public bool Missing { get; set; }
    public bool Unavailable { get; set; }

    public Task<IReadOnlyList<CartOfferSnapshot>?> Read(string slug, CancellationToken ct)
    {
        Reads++;
        if (Unavailable)
            throw new SurubDirect.BuildingBlocks.BusinessException("Catalog unavailable", 503);
        if (Missing)
            return Task.FromResult<IReadOnlyList<CartOfferSnapshot>?>(null);
        return Task.FromResult<IReadOnlyList<CartOfferSnapshot>?>([new(First, VariantId, "Surub demo", "SKU", "Cutie 10", 10, 1, 1, 100, 10m, 12.1m, "RON"), new(Second, VariantId, "Surub demo", "SKU", "Punga 5", 5, 1, 1, 100, 5m, 6.05m, "RON")]);
    }
}
