using SurubDirect.Inventory.Application;
using SurubDirect.ServiceDefaults.Security;

namespace SurubDirect.Inventory.Api.Endpoints;
public static class GetReservationEndpoint
{
    public static void Map(IEndpointRouteBuilder routes, IConfiguration configuration)
    {
        var key = ServiceKey.Required(configuration, "ServiceKeys:Reservations");
        routes.MapGet("/internal/reservations/{id:guid}", async (Guid id, HttpContext context, GetReservationHandler handler, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            if (!ServiceKey.Matches(context.Request, key))
                return Results.Unauthorized();
            return Results.Ok(await handler.Handle(id, ct));
        });
    }
}
