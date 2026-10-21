using SurubDirect.ServiceDefaults.Security;
using SurubDirect.Ordering.Application;

namespace SurubDirect.Ordering.Api.Endpoints;
public static class GetReservationPlanEndpoint
{
    public static void Map(IEndpointRouteBuilder routes, IConfiguration config)
    {
        var key = ServiceKey.Required(config, "ServiceKeys:Coordinator");
        routes.MapGet("/internal/reservation-plans/{id:guid}", async (Guid id, HttpContext context, GetReservationPlanHandler handler, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            if (!ServiceKey.Matches(context.Request, key))
                return Results.Unauthorized();
            return Results.Ok(await handler.Handle(id, ct));
        });
    }
}
