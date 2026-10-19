using SurubDirect.Purchasing.Application;
namespace SurubDirect.Tests;
public sealed class TestPurchasingCatalog : ICatalogVariants
{
    public Task<CatalogVariant?> Find(Guid id, CancellationToken ct) => Task.FromResult<CatalogVariant?>(id == Guid.Empty ? null : new(id, "SKU-" + id.ToString("N"), "M6", "Produs"));
    public Task<IReadOnlyList<CatalogVariant>> Search(string search, CancellationToken ct) => Task.FromResult<IReadOnlyList<CatalogVariant>>([]);
}
