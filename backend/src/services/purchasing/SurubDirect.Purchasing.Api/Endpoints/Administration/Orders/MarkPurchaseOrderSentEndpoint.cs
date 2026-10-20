using SurubDirect.Purchasing.Application;
namespace SurubDirect.Purchasing.Api.Endpoints;
public static class MarkPurchaseOrderSentEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPost("/orders/{id:guid}/sent", async (Guid id, MarkPurchaseOrderSent c, MarkPurchaseOrderSentHandler h, CancellationToken ct) => { await h.Handle(id, c, ct); return Results.NoContent(); });
}
