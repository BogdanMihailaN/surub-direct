using System.Security.Claims;
using SurubDirect.Ordering.Application;
using SurubDirect.ServiceDefaults.Security;
namespace SurubDirect.Ordering.Api.Endpoints;
public static class GetAccountOrdersEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapGet("/api/v1/ordering/account/orders", async (int? page, string? expectedAccountId, HttpContext context, GetAccountOrdersHandler handler, IWebHostEnvironment environment, CancellationToken ct) =>
    {
        context.Response.Headers.CacheControl = "no-store";
        if (!environment.IsDevelopment()) return Results.NotFound();
        if (expectedAccountId is not null && expectedAccountId != context.User.FindFirstValue(ClaimTypes.NameIdentifier))
            return Results.Problem("Contul autentificat s-a schimbat. Reîncarcă pagina contului.", statusCode: 409);
        return Results.Ok(await handler.Handle(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!, page ?? 1, ct));
    }).WithMetadata(new RemoteSessionRequired()).RequireAuthorization().RequireRateLimiting("cart-storage");
}
