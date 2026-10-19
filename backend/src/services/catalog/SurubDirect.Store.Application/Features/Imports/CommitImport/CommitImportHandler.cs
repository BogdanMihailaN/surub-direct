using SurubDirect.BuildingBlocks;
namespace SurubDirect.Store.Application.Imports;
public sealed class CommitImportHandler(ImportBatchBuilder builder, IProductImportRepository repository)
{
    public async Task<ImportResult> Handle(string? csv, CancellationToken ct)
    {
        var batch = await builder.Build(csv, ct);
        if (!batch.Preview.CanImport)
        {
            var error = batch.Preview.Errors.First();
            throw new BusinessException($"Import nesalvat. Linia {error.Line}: {error.Message} Revalidează fișierul.", 409);
        }
        await repository.Save(batch, ct);
        return new(batch.Products.Select(x => x.Id).ToArray(), batch.Preview.VariantCount, batch.Preview.OfferCount);
    }
}
