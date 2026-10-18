using SurubDirect.Catalog.Application;

namespace SurubDirect.Catalog.Api.Endpoints;
public static class CreateProductEndpoint
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapPost("/products", async (CreateProduct command, CreateProductHandler handler, CancellationToken ct) => Results.Ok(new { id = await handler.Handle(command, ct) }));
    }
}
