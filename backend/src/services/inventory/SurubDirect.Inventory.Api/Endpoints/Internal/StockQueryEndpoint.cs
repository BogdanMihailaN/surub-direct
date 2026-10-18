using SurubDirect.Inventory.Contracts;
using SurubDirect.ServiceDefaults.Security;

namespace SurubDirect.Inventory.Api.Endpoints;
public static class StockQueryEndpoint
{
    public static void Map(IEndpointRouteBuilder routes, IConfiguration configuration)
    {
        var key = ServiceKey.Required(configuration, "ServiceKeys:Inventory");
        routes.MapPost("/internal/stock/query", async (StockQuery query, HttpContext context, IInventoryReader reader, CancellationToken ct) =>
        {
            if (!ServiceKey.Matches(context.Request, key))
                return Results.Unauthorized();
            if (query.VariantIds is null || query.VariantIds.Length > 5000)
                return Results.BadRequest();
            return Results.Ok(await reader.GetStock(query.VariantIds, ct));
        });
    }
}
