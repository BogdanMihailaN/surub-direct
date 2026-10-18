using SurubDirect.Catalog.Application;
namespace SurubDirect.Catalog.Api.Endpoints;
public static class UpdateAttributeEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPut("/taxonomy/attributes/{id:guid}",
        async (Guid id, UpdateAttribute command, UpdateAttributeHandler handler, CancellationToken ct) =>
            Results.Ok(new { id = await handler.Handle(id, command, ct) }));
}
