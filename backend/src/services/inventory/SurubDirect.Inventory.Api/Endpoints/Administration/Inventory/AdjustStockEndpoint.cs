using SurubDirect.Inventory.Application;
namespace SurubDirect.Inventory.Api.Endpoints;
public static class AdjustStockEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPost("/variants/{id:guid}/adjustments", async (Guid id, AdjustStock command, AdjustStockHandler handler, HttpContext context, CancellationToken ct) => Results.Ok(new { created = await handler.Handle(id, command, context.User.Identity!.Name!, ct) }));
}
