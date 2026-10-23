using SurubDirect.Inventory.Application;

namespace SurubDirect.Inventory.Api.Endpoints;
public static class GetOutboxStatusEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapGet("/messaging/inventory-outbox", async (HttpContext context, GetOutboxStatusHandler handler, CancellationToken ct) =>
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await handler.Handle(ct));
    });
}
