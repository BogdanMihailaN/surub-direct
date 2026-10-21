using Microsoft.AspNetCore.Antiforgery;

namespace SurubDirect.Ordering.Api.Endpoints;
public static class GetCartTokenEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapGet("/api/v1/ordering/cart/csrf", (HttpContext context, IAntiforgery antiforgery) =>
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(new { token = antiforgery.GetAndStoreTokens(context).RequestToken });
    });
}
