namespace SurubDirect.Identity.Api.Endpoints;
public static class CurrentUserEndpoint
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/auth/me", (HttpContext context) => Results.Ok(new { email = context.User.Identity?.Name, isAdmin = context.User.IsInRole("Administrator") })).RequireAuthorization();
    }
}
