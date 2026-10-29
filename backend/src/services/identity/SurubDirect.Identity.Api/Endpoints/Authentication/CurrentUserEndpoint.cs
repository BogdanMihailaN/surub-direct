using System.Security.Claims;
namespace SurubDirect.Identity.Api.Endpoints;
public static class CurrentUserEndpoint
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/auth/me", (HttpContext context) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            return Results.Ok(new { id = context.User.FindFirstValue(ClaimTypes.NameIdentifier), email = context.User.Identity?.Name, isAdmin = context.User.IsInRole("Administrator") });
        }).RequireAuthorization();
    }
}
