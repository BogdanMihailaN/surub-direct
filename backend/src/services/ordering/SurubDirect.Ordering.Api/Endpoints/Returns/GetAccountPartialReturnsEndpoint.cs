using System.Security.Claims;
using SurubDirect.Ordering.Application;
using SurubDirect.ServiceDefaults.Security;
namespace SurubDirect.Ordering.Api.Endpoints;
public static class GetAccountPartialReturnsEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapGet("/api/v1/ordering/account/orders/{id:guid}/partial-returns", async (Guid id, string? expectedAccountId, HttpContext context, GetAccountPartialReturnsHandler handler, IWebHostEnvironment environment, CancellationToken ct) =>
    {
        context.Response.Headers.CacheControl = "no-store";
        if (!environment.IsDevelopment()) return Results.NotFound();
        var account = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(expectedAccountId)) return Results.Problem("Confirmă contul pentru consultarea retururilor.", statusCode: 400);
        if (account != expectedAccountId) return Results.Problem("Contul autentificat s-a schimbat. Reîncarcă pagina contului.", statusCode: 409);
        return Results.Ok(await handler.Handle(account!, id, ct));
    }).WithMetadata(new RemoteSessionRequired()).RequireAuthorization().RequireRateLimiting("cart-storage");
}
