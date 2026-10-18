using SurubDirect.Inventory.Application;

namespace SurubDirect.Inventory.Api.Endpoints;
public static class ReceiveStockEndpoint
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapPost("/variants/{id:guid}/receipts", async (Guid id, ReceiveStock command, ReceiveStockHandler handler, HttpContext context, CancellationToken ct) => Results.Ok(new { created = await handler.Handle(id, command, context.User.Identity!.Name!, ct) }));
    }
}
