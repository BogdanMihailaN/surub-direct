using SurubDirect.Catalog.Contracts;
using SurubDirect.ServiceDefaults.Security;
namespace SurubDirect.Catalog.Api.Endpoints;
public static class SearchPurchasingVariantsEndpoint
{
    public static void Map(IEndpointRouteBuilder routes, IConfiguration configuration)
    {
        var key = ServiceKey.Required(configuration, "ServiceKeys:Catalog");
        routes.MapGet("/internal/purchasing-variants", async (string? search, HttpContext context, IPurchasingVariantReader reader, CancellationToken ct) =>
        {
            if (!ServiceKey.Matches(context.Request, key)) return Results.Unauthorized();
            if (string.IsNullOrWhiteSpace(search) || search.Trim().Length < 2 || search.Length > 100) return Results.BadRequest();
            return Results.Ok(await reader.Search(search.Trim(), ct));
        });
    }
}
