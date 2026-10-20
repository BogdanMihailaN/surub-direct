using SurubDirect.Inventory.Application;
namespace SurubDirect.Inventory.Api.Endpoints;
public static class GetStockLedgerEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapGet("/variants/{id:guid}/movements", (Guid id, int? page, GetStockLedgerHandler handler, CancellationToken ct) => handler.Handle(id, page ?? 1, ct));
}
