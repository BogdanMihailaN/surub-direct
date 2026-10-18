using Microsoft.AspNetCore.Identity;

namespace SurubDirect.Identity.Api.Endpoints;
public static class LoginEndpoint
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapPost("/api/auth/login", async (LoginRequest request, SignInManager<IdentityUser> signIn) =>
        {
            var result = await signIn.PasswordSignInAsync(request.Email, request.Password, false, true);
            return result.Succeeded ? Results.Ok() : Results.Unauthorized();
        }).RequireRateLimiting("login");
    }
}
