namespace SurubDirect.Catalog.Application;

public sealed class ArchiveProductHandler(ICatalogRepository repository)
{
    public Task Handle(Guid id, CancellationToken ct)
        => repository.Change(id, product => { product.Archive(); return Task.FromResult(true); }, ct);
}
