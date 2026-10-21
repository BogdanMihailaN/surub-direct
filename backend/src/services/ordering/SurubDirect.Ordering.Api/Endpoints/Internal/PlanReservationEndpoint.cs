using SurubDirect.ServiceDefaults.Security;
using SurubDirect.Ordering.Application;

namespace SurubDirect.Ordering.Api.Endpoints;
public static class PlanReservationEndpoint
{
    public static void Map(IEndpointRouteBuilder routes, IConfiguration config)
    {
        var key = ServiceKey.Required(config, "ServiceKeys:Coordinator");
        routes.MapPost("/internal/reservation-plans", async (PlanReservation command, HttpContext context, PlanReservationHandler handler, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            if (!ServiceKey.Matches(context.Request, key))
                return Results.Unauthorized();
            return Results.Ok(await handler.Handle(command, ct));
        });
    }
}
