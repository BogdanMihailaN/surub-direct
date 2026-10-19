using SurubDirect.Catalog.Application;
namespace SurubDirect.Catalog.Api.Endpoints;
public static class UpdateImageEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPut("/products/{id:guid}/images/{imageId:guid}",
        async (Guid id, Guid imageId, UpdateImage command, UpdateImageHandler handler, CancellationToken ct) =>
            Results.Ok(new { id = await handler.Handle(id, imageId, command, ct) }));
}
