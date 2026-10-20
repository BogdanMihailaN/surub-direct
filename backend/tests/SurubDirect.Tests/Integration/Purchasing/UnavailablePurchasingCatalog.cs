using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Application;
namespace SurubDirect.Tests;
public sealed class UnavailablePurchasingCatalog(bool missing) : ICatalogVariants
{
    public Task<CatalogVariant?> Find(Guid id, CancellationToken ct) => missing
        ? Task.FromResult<CatalogVariant?>(null)
        : throw new BusinessException("Catalog indisponibil", 503);
    public Task<IReadOnlyList<CatalogVariant>> Search(string search, CancellationToken ct) => throw new NotSupportedException();
}
