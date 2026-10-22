using SurubDirect.Inventory.Application;
using SurubDirect.ServiceDefaults.Security;

namespace SurubDirect.Inventory.Api.Endpoints;
public static class RestoreStockEndpoint
{
    public static void Map(IEndpointRouteBuilder routes, IConfiguration configuration)
    {
        var key = ServiceKey.Required(configuration, "ServiceKeys:Reservations");
        routes.MapPost("/internal/reservations/{id:guid}/restore", async (Guid id, RestoreStock command, HttpContext context, RestoreStockHandler handler, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            if (!ServiceKey.Matches(context.Request, key))
                return Results.Unauthorized();
            return Results.Ok(await handler.Handle(id, command, ct));
        });
    }
}
