using Microsoft.AspNetCore.Antiforgery;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Api.Security;

namespace SurubDirect.Ordering.Api.Endpoints;
public static class CompleteDemoOrderEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPost("/api/v1/ordering/cart/preparation/{id:guid}/complete", async (Guid id, HttpContext context, IAntiforgery antiforgery, CartOwnerCookie owner, CompleteDemoOrderHandler handler, IWebHostEnvironment environment, CancellationToken ct) =>
    {
        if (!environment.IsDevelopment())
            return Results.NotFound();
        context.Response.Headers.CacheControl = "no-store";
        await antiforgery.ValidateRequestAsync(context);
        await handler.Handle(owner.Require(context), id, ct);
        return Results.NoContent();
    }).RequireRateLimiting("cart-storage");
}
