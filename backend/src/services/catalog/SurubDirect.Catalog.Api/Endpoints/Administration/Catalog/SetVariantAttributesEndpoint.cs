using SurubDirect.Catalog.Application;
namespace SurubDirect.Catalog.Api.Endpoints;
public static class SetVariantAttributesEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPut("/products/{id:guid}/variants/{variantId:guid}/attributes",
        async (Guid id, Guid variantId, SetVariantAttributeValue[] command, SetVariantAttributesHandler handler, CancellationToken ct) =>
        { await handler.Handle(id, variantId, command, ct); return Results.NoContent(); });
}
