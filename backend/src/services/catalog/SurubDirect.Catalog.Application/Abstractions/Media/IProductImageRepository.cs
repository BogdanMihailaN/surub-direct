using SurubDirect.Catalog.Domain;
namespace SurubDirect.Catalog.Application;
public interface IProductImageRepository
{
    Task<T> Change<T>(Guid productId, Func<Product, Task<T>> operation, CancellationToken ct);
    Task<bool> Exists(Guid productId, Guid imageId, bool includeDrafts, CancellationToken ct);
}
