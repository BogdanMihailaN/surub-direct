using Microsoft.AspNetCore.Antiforgery;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Api.Security;

namespace SurubDirect.Ordering.Api.Endpoints;
public static class ReplaceCartEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPut("/api/v1/ordering/cart", async (ReplaceCart command, HttpContext context, IAntiforgery antiforgery, CartOwnerCookie owner, ReplaceCartHandler handler, CancellationToken ct) =>
    {
        await antiforgery.ValidateRequestAsync(context);
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await handler.Handle(owner.Require(context), command, ct));
    }).RequireRateLimiting("cart-storage");
}
