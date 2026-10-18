using SurubDirect.Catalog.Application;

namespace SurubDirect.Catalog.Api.Endpoints;
public static class AddVariantEndpoint
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapPost("/products/{id:guid}/variants", async (Guid id, AddVariant command, AddVariantHandler handler, CancellationToken ct) => Results.Ok(new { id = await handler.Handle(id, command, ct) }));
    }
}
