using SurubDirect.Catalog.Application;
namespace SurubDirect.Catalog.Api.Endpoints;
public static class UpdateCategoryEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPut("/taxonomy/categories/{id:guid}",
        async (Guid id, UpdateCategory command, UpdateCategoryHandler handler, CancellationToken ct) =>
            Results.Ok(new { id = await handler.Handle(id, command, ct) }));
}
