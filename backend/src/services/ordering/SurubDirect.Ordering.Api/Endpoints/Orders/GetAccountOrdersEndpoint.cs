using System.Security.Claims;
using SurubDirect.Ordering.Application;
using SurubDirect.ServiceDefaults.Security;
namespace SurubDirect.Ordering.Api.Endpoints;
public static class GetAccountOrdersEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapGet("/api/v1/ordering/account/orders", async (int? page, HttpContext context, GetAccountOrdersHandler handler, IWebHostEnvironment environment, CancellationToken ct) =>
    {
        context.Response.Headers.CacheControl = "no-store";
        if (!environment.IsDevelopment()) return Results.NotFound();
        return Results.Ok(await handler.Handle(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!, page ?? 1, ct));
    }).WithMetadata(new RemoteSessionRequired()).RequireAuthorization().RequireRateLimiting("cart-storage");
}
