using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Domain;
namespace SurubDirect.Purchasing.Application;
public sealed class SearchVariantsHandler(ICatalogVariants catalog)
{
    public Task<IReadOnlyList<CatalogVariant>> Handle(string? search, CancellationToken ct)
    {
        var query = Guard.Text(search, "Caută SKU sau denumire", 100);
        if (query.Length < 2) throw new BusinessException("Introdu minimum două caractere pentru căutare.");
        return catalog.Search(query, ct);
    }
}
