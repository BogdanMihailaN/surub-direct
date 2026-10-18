using SurubDirect.Store.Application;

namespace SurubDirect.Catalog.Api.Endpoints;
public static class GetAdminProductEndpoint
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapGet("/products/{id:guid}", async (Guid id, GetAdminProductHandler handler, CancellationToken ct) =>
        {
            var product = await handler.Handle(id, ct);
            return product is null ? Results.NotFound() : Results.Ok(product);
        });
    }
}
