using SurubDirect.Purchasing.Application;
namespace SurubDirect.Purchasing.Api.Endpoints;
public static class SearchVariantsEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapGet("/variants", (string? search, SearchVariantsHandler handler, CancellationToken ct) => handler.Handle(search, ct));
}
