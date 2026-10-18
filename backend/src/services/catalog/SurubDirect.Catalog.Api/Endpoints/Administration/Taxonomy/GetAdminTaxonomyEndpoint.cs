using SurubDirect.Catalog.Contracts;
namespace SurubDirect.Catalog.Api.Endpoints;
public static class GetAdminTaxonomyEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapGet("/taxonomy", (ITaxonomyReader reader, CancellationToken ct) => reader.Get(ct, true));
}
