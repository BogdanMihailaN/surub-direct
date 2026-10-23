using SurubDirect.Ordering.Application;

namespace SurubDirect.Ordering.Api.Endpoints;
public static class GetBrokerStatusEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapGet("/api/v1/admin/messaging/broker", async (HttpContext context, GetBrokerStatusHandler handler, CancellationToken ct) =>
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await handler.Handle(ct));
    }).RequireAuthorization("Admin");
}
