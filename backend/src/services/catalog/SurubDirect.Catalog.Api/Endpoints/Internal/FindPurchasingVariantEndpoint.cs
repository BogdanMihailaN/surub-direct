using SurubDirect.Catalog.Contracts;
using SurubDirect.ServiceDefaults.Security;
namespace SurubDirect.Catalog.Api.Endpoints;
public static class FindPurchasingVariantEndpoint
{
    public static void Map(IEndpointRouteBuilder routes, IConfiguration configuration)
    {
        var key = ServiceKey.Required(configuration, "ServiceKeys:Catalog");
        routes.MapGet("/internal/purchasing-variants/{id:guid}", async (Guid id, HttpContext context, IPurchasingVariantReader reader, CancellationToken ct) =>
        {
            if (!ServiceKey.Matches(context.Request, key)) return Results.Unauthorized();
            return await reader.Find(id, ct) is { } variant ? Results.Ok(variant) : Results.NotFound();
        });
    }
}
