using SurubDirect.Catalog.Application;
namespace SurubDirect.Catalog.Api.Endpoints;
public static class CreateCategoryEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPost("/taxonomy/categories",
        async (CreateCategory command, CreateCategoryHandler handler, CancellationToken ct) =>
            Results.Ok(new { id = await handler.Handle(command, ct) }));
}
