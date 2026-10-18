namespace SurubDirect.Catalog.Contracts;
public interface ICatalogReader
{
    Task<CatalogPage> List(CatalogFilter filter, bool includeDrafts, CancellationToken ct);
    Task<ProductDto?> Get(Guid id, bool includeDrafts, CancellationToken ct);
    Task<ProductDto?> GetBySlug(string slug, CancellationToken ct);
    Task<IReadOnlyList<string>> Categories(CancellationToken ct);
    Task<bool> VariantExists(Guid id, CancellationToken ct);
    Task<bool> OfferExists(Guid id, CancellationToken ct);
}
