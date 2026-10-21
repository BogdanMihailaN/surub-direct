using Microsoft.AspNetCore.Antiforgery;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Api.Security;

namespace SurubDirect.Ordering.Api.Endpoints;
public static class SaveCartCustomerEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPut("/api/v1/ordering/cart/customer", async (SaveCartCustomer command, HttpContext context, IAntiforgery antiforgery, CartOwnerCookie owner, SaveCartCustomerHandler handler, CancellationToken ct) =>
    {
        context.Response.Headers.CacheControl = "no-store";
        await antiforgery.ValidateRequestAsync(context);
        return Results.Ok(await handler.Handle(owner.Require(context), command, ct));
    }).RequireRateLimiting("cart-storage");
}
