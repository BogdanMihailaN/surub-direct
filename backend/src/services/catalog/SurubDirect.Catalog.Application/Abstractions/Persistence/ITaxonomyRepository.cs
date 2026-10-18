using SurubDirect.Catalog.Domain;

namespace SurubDirect.Catalog.Application;
public interface ITaxonomyRepository
{
    Task<List<Category>> Categories(CancellationToken ct);
    Task<List<TechnicalAttribute>> Attributes(CancellationToken ct);
    Task<Category> ResolveCategory(Guid? id, string legacyName, CancellationToken ct);
    void Add(Category category);
    void Add(TechnicalAttribute attribute);
    Task<T> Change<T>(Func<Task<T>> operation, CancellationToken ct);
}
