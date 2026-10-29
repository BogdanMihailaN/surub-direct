using System.Security.Claims;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Api.Security;
using SurubDirect.ServiceDefaults.Security;
namespace SurubDirect.Ordering.Api.Endpoints;
public static class LinkAccountOrderEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPost("/api/v1/ordering/account/orders/{id:guid}/claim", async (Guid id, HttpContext context, CartOwnerCookie owner, LinkAccountOrderHandler handler, IWebHostEnvironment environment, CancellationToken ct) =>
    {
        context.Response.Headers.CacheControl = "no-store";
        if (!environment.IsDevelopment()) return Results.NotFound();
        await handler.Handle(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!, owner.Require(context), id, ct);
        return Results.NoContent();
    }).WithMetadata(new RemoteSessionRequired()).RequireAuthorization().RequireRateLimiting("cart-storage");
}
