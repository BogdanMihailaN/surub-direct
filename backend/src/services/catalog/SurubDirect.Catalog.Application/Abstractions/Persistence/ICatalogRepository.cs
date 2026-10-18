using SurubDirect.Catalog.Domain;

namespace SurubDirect.Catalog.Application;
public interface ICatalogRepository
{
    Task<Product?> Get(Guid id, CancellationToken ct);
    Task Add(Product product, CancellationToken ct);
    Task Save(CancellationToken ct);
    void RecordPublication(Product product);
}
