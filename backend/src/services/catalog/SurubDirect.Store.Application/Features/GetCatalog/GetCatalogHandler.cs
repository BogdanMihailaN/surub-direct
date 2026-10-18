using SurubDirect.BuildingBlocks;
using SurubDirect.Catalog.Contracts;

namespace SurubDirect.Store.Application;
public sealed class GetCatalogHandler(ICatalogReader catalog, ProductEnricher enricher)
{
    public async Task<StorePage> Handle(CatalogFilter filter, bool includeDrafts, CancellationToken ct)
    {
        if (filter.Page < 1 || filter.Page > 10000 || filter.PageSize < 1 || filter.PageSize > 100 || filter.Search?.Length > 100)
            throw new BusinessException("Paginare sau căutare invalidă.");
        if (filter.Status is not null && filter.Status is not ("Draft" or "Published" or "Archived"))
            throw new BusinessException("Starea produsului nu este validă.");
        var page = await catalog.List(filter, includeDrafts, ct);
        return new(await enricher.Enrich(page.Items, ct), page.Total, page.Page, page.PageSize);
    }
}
