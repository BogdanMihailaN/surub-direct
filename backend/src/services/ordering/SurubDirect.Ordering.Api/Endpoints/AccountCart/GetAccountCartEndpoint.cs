using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Api.Security;
using SurubDirect.ServiceDefaults.Security;
namespace SurubDirect.Ordering.Api.Endpoints;
public static class GetAccountCartEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapGet("/api/v1/ordering/account/cart", async (string expectedAccountId, HttpContext context, GetAccountCartHandler handler, IWebHostEnvironment environment, CancellationToken ct) =>
    {
        context.Response.Headers.CacheControl = "no-store";
        if (!environment.IsDevelopment()) return Results.NotFound();
        var accountId = AccountCartIdentity.Require(context, expectedAccountId);
        var cart = await handler.Handle(accountId, ct);
        return cart is null ? Results.NoContent() : Results.Ok(cart);
    }).WithMetadata(new RemoteSessionRequired()).RequireAuthorization().RequireRateLimiting("cart-storage");
}
