using SurubDirect.Catalog.Application;
namespace SurubDirect.Catalog.Api.Endpoints;
public static class CreateAttributeEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPost("/taxonomy/attributes",
        async (CreateAttribute command, CreateAttributeHandler handler, CancellationToken ct) =>
            Results.Ok(new { id = await handler.Handle(command, ct) }));
}
