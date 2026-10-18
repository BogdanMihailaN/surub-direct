using SurubDirect.Store.Application;

namespace SurubDirect.Catalog.Api.Endpoints;
public static class ListAdminProductsEndpoint
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapGet("/products", (string? search, string? status, int? page, int? pageSize, GetCatalogHandler handler, CancellationToken ct) => handler.Handle(new(Search: search, Page: page ?? 1, PageSize: pageSize ?? 25, Status: status), true, ct));
    }
}
