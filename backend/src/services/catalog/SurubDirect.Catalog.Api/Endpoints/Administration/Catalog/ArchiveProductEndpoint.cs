using SurubDirect.Catalog.Application;

namespace SurubDirect.Catalog.Api.Endpoints;
public static class ArchiveProductEndpoint
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapPost("/products/{id:guid}/archive", async (Guid id, ArchiveProductHandler handler, CancellationToken ct) =>
        {
            await handler.Handle(id, ct);
            return Results.NoContent();
        });
    }
}
