using SurubDirect.Purchasing.Application;
namespace SurubDirect.Purchasing.Api.Endpoints;
public static class RemovePurchaseOrderLineEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPost("/orders/{id:guid}/lines/{lineId:guid}/remove", async (Guid id, Guid lineId, RemovePurchaseOrderLine c, RemovePurchaseOrderLineHandler h, CancellationToken ct) => { await h.Handle(id, lineId, c, ct); return Results.NoContent(); });
}
