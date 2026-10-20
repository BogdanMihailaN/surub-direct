using SurubDirect.Purchasing.Application;
namespace SurubDirect.Purchasing.Api.Endpoints;
public static class CancelPurchaseOrderEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPost("/orders/{id:guid}/cancel", async (Guid id, CancelPurchaseOrder c, CancelPurchaseOrderHandler h, CancellationToken ct) => { await h.Handle(id, c, ct); return Results.NoContent(); });
}
