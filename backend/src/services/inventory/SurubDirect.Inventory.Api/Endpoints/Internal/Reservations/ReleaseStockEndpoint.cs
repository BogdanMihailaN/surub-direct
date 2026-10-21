using SurubDirect.Inventory.Application;
using SurubDirect.ServiceDefaults.Security;

namespace SurubDirect.Inventory.Api.Endpoints;
public static class ReleaseStockEndpoint
{
    public static void Map(IEndpointRouteBuilder routes, IConfiguration configuration)
    {
        var key = ServiceKey.Required(configuration, "ServiceKeys:Reservations");
        routes.MapPost("/internal/reservations/{id:guid}/release", async (Guid id, ReleaseStock command, HttpContext context, ReleaseStockHandler handler, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            if (!ServiceKey.Matches(context.Request, key))
                return Results.Unauthorized();
            return Results.Ok(await handler.Handle(id, command, ct));
        });
    }
}
