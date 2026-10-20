using SurubDirect.Ordering.Application;
namespace SurubDirect.Ordering.Api.Endpoints;

public static class QuoteCartEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPost("/api/v1/ordering/cart/quote", async (QuoteCart command, QuoteCartHandler handler, HttpContext context, CancellationToken ct) => { context.Response.Headers.CacheControl = "no-store"; return Results.Ok(await handler.Handle(command, ct)); }).RequireRateLimiting("cart-quotes");
}
