using Microsoft.AspNetCore.Antiforgery;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Api.Security;

namespace SurubDirect.Ordering.Api.Endpoints;
public static class GetPreparationEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapGet("/api/v1/ordering/cart/preparation", async (HttpContext context, IAntiforgery antiforgery, CartOwnerCookie owner, GetPreparationHandler handler, IWebHostEnvironment environment, CancellationToken ct) =>
    {
        if (!environment.IsDevelopment())
            return Results.NotFound();
        context.Response.Headers.CacheControl = "no-store";
        var result = await handler.Handle(owner.Require(context), ct);
        return result is null ? Results.NoContent() : Results.Ok(result);
    }).RequireRateLimiting("cart-storage");
}
