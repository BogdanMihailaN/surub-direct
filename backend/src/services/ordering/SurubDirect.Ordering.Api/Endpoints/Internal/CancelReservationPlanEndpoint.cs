using SurubDirect.ServiceDefaults.Security;
using SurubDirect.Ordering.Application;

namespace SurubDirect.Ordering.Api.Endpoints;
public static class CancelReservationPlanEndpoint
{
    public static void Map(IEndpointRouteBuilder routes, IConfiguration config)
    {
        var key = ServiceKey.Required(config, "ServiceKeys:Coordinator");
        routes.MapPost("/internal/reservation-plans/{id:guid}/cancel", async (Guid id, CancelReservationPlanRequest command, HttpContext context, CancelReservationHandler handler, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            if (!ServiceKey.Matches(context.Request, key))
                return Results.Unauthorized();
            return Results.Ok(await handler.Handle(id, command.OrderId, ct));
        });
    }
}
