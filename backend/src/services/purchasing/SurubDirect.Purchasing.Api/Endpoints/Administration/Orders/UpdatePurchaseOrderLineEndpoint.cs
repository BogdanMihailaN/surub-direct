using SurubDirect.Purchasing.Application;
namespace SurubDirect.Purchasing.Api.Endpoints;
public static class UpdatePurchaseOrderLineEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPut("/orders/{id:guid}/lines/{lineId:guid}", async (Guid id, Guid lineId, UpdatePurchaseOrderLine c, UpdatePurchaseOrderLineHandler h, CancellationToken ct) => { await h.Handle(id, lineId, c, ct); return Results.NoContent(); });
}
