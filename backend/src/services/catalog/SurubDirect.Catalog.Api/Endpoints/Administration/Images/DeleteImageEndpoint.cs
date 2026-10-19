using SurubDirect.Catalog.Application;
namespace SurubDirect.Catalog.Api.Endpoints;
public static class DeleteImageEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapDelete("/products/{id:guid}/images/{imageId:guid}",
        async (Guid id, Guid imageId, DeleteImageHandler handler, CancellationToken ct) =>
        {
            await handler.Handle(id, imageId, ct);
            return Results.NoContent();
        });
}
