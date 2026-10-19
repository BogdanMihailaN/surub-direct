namespace SurubDirect.Purchasing.Application;
public interface ICatalogVariants
{
    Task<CatalogVariant?> Find(Guid id, CancellationToken ct);
    Task<IReadOnlyList<CatalogVariant>> Search(string search, CancellationToken ct);
}
