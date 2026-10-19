using SurubDirect.Purchasing.Application;
namespace SurubDirect.Purchasing.Api.Endpoints;
public static class GetSupplierEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapGet("/suppliers/{id:guid}", (Guid id, GetSupplierHandler handler, CancellationToken ct) => handler.Handle(id, ct));
}
