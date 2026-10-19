namespace SurubDirect.Store.Application.Imports;
public sealed class PreviewImportHandler(ImportBatchBuilder builder)
{
    public async Task<ImportPreview> Handle(string? csv, CancellationToken ct) => (await builder.Build(csv, ct)).Preview;
}
