using SurubDirect.Catalog.Application;

namespace SurubDirect.Catalog.Api.Endpoints;
public static class RestoreProductEndpoint
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapPost("/products/{id:guid}/restore", async (Guid id, RestoreProductHandler handler, CancellationToken ct) =>
        {
            await handler.Handle(id, ct);
            return Results.NoContent();
        });
    }
}
