using SurubDirect.Catalog.Application;

namespace SurubDirect.Catalog.Api.Endpoints;
public static class UpdateProductEndpoint
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapPut("/products/{id:guid}", async (Guid id, UpdateProduct command, UpdateProductHandler handler, CancellationToken ct) =>
        {
            await handler.Handle(id, command, ct);
            return Results.NoContent();
        });
    }
}
