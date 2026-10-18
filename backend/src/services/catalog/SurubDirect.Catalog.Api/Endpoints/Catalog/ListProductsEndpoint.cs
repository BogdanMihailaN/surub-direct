using SurubDirect.Store.Application;

namespace SurubDirect.Catalog.Api.Endpoints;
public static class ListProductsEndpoint
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/v1/products", (string? search, string? category, Guid? categoryId, string? attributes, decimal? diameter, decimal? length, int? page, int? pageSize, GetCatalogHandler handler, CancellationToken ct) => handler.Handle(new(search, category, diameter, length, page ?? 1, pageSize ?? 12, CategoryId: categoryId, Attributes: AttributeFilterParser.Parse(attributes)), false, ct));
    }
}
