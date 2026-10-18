namespace SurubDirect.Catalog.Application;
public interface ICatalogOutbox
{
    Task<int> ProcessBatch(CancellationToken ct);
}
