namespace SurubDirect.Store.Application.Imports;
public interface IProductImportRepository
{
    Task<ImportLookup> Lookup(IReadOnlyList<ImportRow> rows, CancellationToken ct);
    Task Save(ImportBatch batch, CancellationToken ct);
}
