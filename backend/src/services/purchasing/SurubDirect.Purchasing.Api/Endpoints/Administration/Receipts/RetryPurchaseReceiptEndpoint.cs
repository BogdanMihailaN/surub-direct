using SurubDirect.Purchasing.Application;
namespace SurubDirect.Purchasing.Api.Endpoints;
public static class RetryPurchaseReceiptEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPost("/orders/{id:guid}/receipts/{receiptId:guid}/retry", async (Guid id, Guid receiptId, RetryPurchaseReceiptHandler h, CancellationToken ct) => { await h.Handle(id, receiptId, ct); return Results.NoContent(); });
}
