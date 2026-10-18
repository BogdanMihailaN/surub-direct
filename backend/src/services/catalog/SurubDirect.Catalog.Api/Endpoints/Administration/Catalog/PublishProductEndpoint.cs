using SurubDirect.Store.Application;

namespace SurubDirect.Catalog.Api.Endpoints;
public static class PublishProductEndpoint
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapPost("/products/{id:guid}/publish", async (Guid id, PublishStoreProductHandler handler, CancellationToken ct) =>
        {
            await handler.Handle(id, ct);
            return Results.NoContent();
        });
    }
}
