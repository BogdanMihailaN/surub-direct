using SurubDirect.Purchasing.Application;
namespace SurubDirect.Purchasing.Api.Endpoints;

public static class ClosePurchaseOrderEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPost("/orders/{id:guid}/close", async (Guid id, ClosePurchaseOrder command, ClosePurchaseOrderHandler handler, HttpContext context, CancellationToken ct) => { await handler.Handle(id, command, context.User.Identity!.Name!, ct); return Results.NoContent(); });
}
