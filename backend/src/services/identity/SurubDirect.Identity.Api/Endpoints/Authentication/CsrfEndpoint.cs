using Microsoft.AspNetCore.Antiforgery;

namespace SurubDirect.Identity.Api.Endpoints;
public static class CsrfEndpoint
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/auth/csrf", (HttpContext context, IAntiforgery antiforgery) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            return Results.Ok(new { token = antiforgery.GetAndStoreTokens(context).RequestToken });
        });
    }
}
