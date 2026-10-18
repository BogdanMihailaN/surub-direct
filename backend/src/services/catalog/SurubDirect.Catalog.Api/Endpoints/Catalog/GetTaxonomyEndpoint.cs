using SurubDirect.Catalog.Contracts;
namespace SurubDirect.Catalog.Api.Endpoints;
public static class GetTaxonomyEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapGet("/api/v1/taxonomy", (ITaxonomyReader reader, CancellationToken ct) => reader.Get(ct));
}
