using SurubDirect.Purchasing.Application;
namespace SurubDirect.Purchasing.Api.Endpoints;
public static class GetPurchaseOrderEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapGet("/orders/{id:guid}", (Guid id, GetPurchaseOrderHandler h, CancellationToken ct) => h.Handle(id, ct));
}
