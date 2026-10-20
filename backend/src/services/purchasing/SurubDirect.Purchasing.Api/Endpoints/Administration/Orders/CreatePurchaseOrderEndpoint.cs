using SurubDirect.Purchasing.Application;
namespace SurubDirect.Purchasing.Api.Endpoints;
public static class CreatePurchaseOrderEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPost("/orders", async (CreatePurchaseOrder c, CreatePurchaseOrderHandler h, CancellationToken ct) => { var id = await h.Handle(c, ct); return Results.Created($"/api/v1/admin/purchasing/orders/{id}", new { id }); });
}
