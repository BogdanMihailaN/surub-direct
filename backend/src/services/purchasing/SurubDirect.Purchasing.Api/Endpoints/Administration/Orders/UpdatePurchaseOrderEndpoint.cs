using SurubDirect.Purchasing.Application;
namespace SurubDirect.Purchasing.Api.Endpoints;
public static class UpdatePurchaseOrderEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPut("/orders/{id:guid}", async (Guid id, UpdatePurchaseOrder c, UpdatePurchaseOrderHandler h, CancellationToken ct) => { await h.Handle(id, c, ct); return Results.NoContent(); });
}
