using SurubDirect.Catalog.Contracts;
using SurubDirect.ServiceDefaults.Security;

namespace SurubDirect.Catalog.Api.Endpoints;
public static class VariantExistsEndpoint
{
    public static void Map(IEndpointRouteBuilder routes, IConfiguration configuration)
    {
        var key = ServiceKey.Required(configuration, "ServiceKeys:Catalog");
        routes.MapGet("/internal/variants/{id:guid}", async (Guid id, HttpContext context, ICatalogReader reader, CancellationToken ct) =>
        {
            if (!ServiceKey.Matches(context.Request, key))
                return Results.Unauthorized();
            return await reader.VariantExists(id, ct) ? Results.Ok() : Results.NotFound();
        });
    }
}
