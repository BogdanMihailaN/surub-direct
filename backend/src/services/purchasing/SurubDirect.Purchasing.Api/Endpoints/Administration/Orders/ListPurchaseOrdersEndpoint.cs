using SurubDirect.Purchasing.Application;
namespace SurubDirect.Purchasing.Api.Endpoints;
public static class ListPurchaseOrdersEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapGet("/orders", (string? search, string? status, int? page, ListPurchaseOrdersHandler h, CancellationToken ct) => h.Handle(search, status, page ?? 1, ct));
}
