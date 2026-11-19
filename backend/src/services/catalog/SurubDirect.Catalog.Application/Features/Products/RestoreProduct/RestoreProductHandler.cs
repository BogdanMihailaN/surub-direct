namespace SurubDirect.Catalog.Application;

public sealed class RestoreProductHandler(ICatalogRepository repository)
{
    public Task Handle(Guid id, CancellationToken ct)
        => repository.Change(id, product => { product.Restore(); return Task.FromResult(true); }, ct);
}
