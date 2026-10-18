using Microsoft.AspNetCore.Identity;

namespace SurubDirect.Identity.Api.Endpoints;
public static class LogoutEndpoint
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapPost("/api/auth/logout", async (SignInManager<IdentityUser> signIn) =>
        {
            await signIn.SignOutAsync();
            return Results.NoContent();
        }).RequireAuthorization();
    }
}
