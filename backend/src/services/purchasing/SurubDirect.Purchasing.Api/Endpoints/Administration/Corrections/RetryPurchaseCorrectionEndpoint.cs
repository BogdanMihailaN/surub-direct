using SurubDirect.Purchasing.Application;
namespace SurubDirect.Purchasing.Api.Endpoints;

public static class RetryPurchaseCorrectionEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPost("/orders/{id:guid}/corrections/{correctionId:guid}/retry", async (Guid id, Guid correctionId, RetryPurchaseCorrectionHandler handler, CancellationToken ct) => { await handler.Handle(id, correctionId, ct); return Results.NoContent(); });
}
