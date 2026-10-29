using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Api.Security;
using SurubDirect.ServiceDefaults.Security;
namespace SurubDirect.Ordering.Api.Endpoints;
public static class ReplaceAccountCartEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPut("/api/v1/ordering/account/cart", async (ReplaceCart command, string expectedAccountId, HttpContext context, ReplaceAccountCartHandler handler, IWebHostEnvironment environment, CancellationToken ct) =>
    {
        context.Response.Headers.CacheControl = "no-store";
        if (!environment.IsDevelopment()) return Results.NotFound();
        var accountId = AccountCartIdentity.Require(context, expectedAccountId);
        var cart = await handler.Handle(accountId, command, ct);
        return Results.Ok(cart);
    }).WithMetadata(new RemoteSessionRequired()).RequireAuthorization().RequireRateLimiting("cart-storage");
}
