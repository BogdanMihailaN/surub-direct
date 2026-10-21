using Microsoft.AspNetCore.Antiforgery;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Api.Security;

namespace SurubDirect.Ordering.Api.Endpoints;
public static class PrepareCartEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPost("/api/v1/ordering/cart/preparation", async (PrepareCart command, HttpContext context, IAntiforgery antiforgery, CartOwnerCookie owner, PrepareCartHandler handler, IWebHostEnvironment environment, CancellationToken ct) =>
    {
        if (!environment.IsDevelopment())
            return Results.NotFound();
        context.Response.Headers.CacheControl = "no-store";
        await antiforgery.ValidateRequestAsync(context);
        return Results.Ok(await handler.Handle(owner.Require(context), command, ct));
    }).RequireRateLimiting("cart-storage");
}
