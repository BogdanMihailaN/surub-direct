using SurubDirect.Purchasing.Application;
namespace SurubDirect.Purchasing.Api.Endpoints;
public static class ListSuppliersEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapGet("/suppliers", (string? search, bool? active, int? page, ListSuppliersHandler handler, CancellationToken ct) => handler.Handle(search, active, page ?? 1, ct));
}
