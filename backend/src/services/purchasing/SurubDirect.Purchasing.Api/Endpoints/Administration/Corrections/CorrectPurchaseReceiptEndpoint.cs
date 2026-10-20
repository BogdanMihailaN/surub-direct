using SurubDirect.Purchasing.Application;
namespace SurubDirect.Purchasing.Api.Endpoints;

public static class CorrectPurchaseReceiptEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPost("/orders/{id:guid}/corrections", async (Guid id, CorrectPurchaseReceipt c, CorrectPurchaseReceiptHandler handler, HttpContext context, CancellationToken ct) => { await handler.Handle(id, c, context.User.Identity!.Name!, ct); return Results.NoContent(); });
}
