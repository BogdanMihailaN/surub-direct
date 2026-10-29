using SurubDirect.Identity.Application.Features.RegisterCustomer;

namespace SurubDirect.Identity.Api.Endpoints;
public static class RegisterCustomerEndpoint
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapPost("/api/auth/register", async (RegisterCustomerRequest request, HttpContext context,
            IWebHostEnvironment environment, RegisterCustomerHandler handler) =>
        {
            if (!environment.IsDevelopment())
                return Results.NotFound();
            if (context.User.Identity?.IsAuthenticated == true)
                return Results.StatusCode(StatusCodes.Status409Conflict);
            context.Response.Headers.CacheControl = "no-store";
            return await handler.Handle(request.Email, request.Password)
                ? Results.NoContent()
                : Results.Problem("Introdu o adresă de e-mail validă și o parolă de 12–128 de caractere, cu literă mare, literă mică, cifră și simbol.", statusCode: 400);
        }).RequireRateLimiting("registration");
    }
}
