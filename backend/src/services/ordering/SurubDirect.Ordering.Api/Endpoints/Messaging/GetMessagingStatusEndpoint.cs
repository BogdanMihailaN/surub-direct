using System.Security.Claims;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Api.Endpoints;
public static class GetMessagingStatusEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapGet("/api/v1/admin/messaging", async (int? page, HttpContext context, GetMessagingStatusHandler handler, CancellationToken ct) =>
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await handler.Handle(page ?? 1, ct));
    }).RequireAuthorization("Admin");
}
