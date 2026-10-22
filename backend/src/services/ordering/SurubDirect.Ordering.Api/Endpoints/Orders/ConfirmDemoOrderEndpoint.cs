using Microsoft.AspNetCore.Antiforgery;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Api.Security;

namespace SurubDirect.Ordering.Api.Endpoints;
public static class ConfirmDemoOrderEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPost("/api/v1/ordering/cart/preparation/{id:guid}/confirm", async (Guid id, HttpContext context, IAntiforgery antiforgery, CartOwnerCookie owner, ConfirmDemoOrderHandler handler, IWebHostEnvironment environment, CancellationToken ct) =>
    {
        if (!environment.IsDevelopment())
            return Results.NotFound();
        context.Response.Headers.CacheControl = "no-store";
        await antiforgery.ValidateRequestAsync(context);
        return Results.Ok(await handler.Handle(owner.Require(context), id, ct));
    }).RequireRateLimiting("cart-storage");
}
