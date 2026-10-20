using SurubDirect.Purchasing.Application;
namespace SurubDirect.Purchasing.Api.Endpoints;
public static class ReceivePurchaseOrderEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPost("/orders/{id:guid}/receipts", async (Guid id, ReceivePurchaseOrder c, ReceivePurchaseOrderHandler h, HttpContext context, CancellationToken ct) => { await h.Handle(id, c, context.User.Identity!.Name!, ct); return Results.NoContent(); });
}
