using SurubDirect.Store.Application;

namespace SurubDirect.Catalog.Api.Endpoints;
public static class GetProductEndpoint
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/v1/products/{slug}", async (string slug, GetProductHandler handler, CancellationToken ct) => await handler.Handle(slug, ct)is { } product ? Results.Ok(product) : Results.NotFound());
    }
}
