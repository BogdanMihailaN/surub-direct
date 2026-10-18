using SurubDirect.Catalog.Contracts;

namespace SurubDirect.Catalog.Api.Endpoints;
public static class ListCategoriesEndpoint
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/v1/categories", (ICatalogReader catalog, CancellationToken ct) => catalog.Categories(ct));
    }
}
