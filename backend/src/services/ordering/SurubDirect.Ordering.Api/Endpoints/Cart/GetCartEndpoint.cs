using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Api.Security;

namespace SurubDirect.Ordering.Api.Endpoints;
public static class GetCartEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapGet("/api/v1/ordering/cart", async (HttpContext context, CartOwnerCookie owner, GetCartHandler handler, CancellationToken ct) =>
    {
        context.Response.Headers.CacheControl = "no-store";
        var hash = owner.Read(context);
        var cart = hash is null ? null : await handler.Handle(hash, ct);
        return cart is null ? Results.NoContent() : Results.Ok(cart);
    }).RequireRateLimiting("cart-storage");
}
