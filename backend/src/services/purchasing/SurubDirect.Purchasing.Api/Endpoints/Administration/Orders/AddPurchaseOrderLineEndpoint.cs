using SurubDirect.Purchasing.Application;
namespace SurubDirect.Purchasing.Api.Endpoints;
public static class AddPurchaseOrderLineEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPost("/orders/{id:guid}/lines", async (Guid id, AddPurchaseOrderLine c, AddPurchaseOrderLineHandler h, CancellationToken ct) => { await h.Handle(id, c, ct); return Results.NoContent(); });
}
