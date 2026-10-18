using SurubDirect.Catalog.Application;

namespace SurubDirect.Catalog.Api.Endpoints;
public static class UpdateVariantEndpoint
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapPut("/products/{id:guid}/variants/{variantId:guid}", async (Guid id, Guid variantId, UpdateVariant command, UpdateVariantHandler handler, CancellationToken ct) =>
        {
            await handler.Handle(id, variantId, command, ct);
            return Results.NoContent();
        });
    }
}
