using SurubDirect.Identity.Application.Features.Recovery;
namespace SurubDirect.Identity.Api.Endpoints;
public static class RequestPasswordResetEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPost("/api/auth/request-password-reset", async (RequestRecoveryRequest request, HttpContext context, IWebHostEnvironment environment, RequestPasswordResetHandler handler) =>
    {
        context.Response.Headers.CacheControl = "no-store";
        if (!environment.IsDevelopment()) return Results.NotFound();
        return await handler.Handle(request.Email) ? Results.NoContent() : Results.Problem("Date invalide sau token expirat. Solicită un mesaj nou și verifică datele introduse.", statusCode: 400);
    }).RequireRateLimiting("account-recovery");
}
