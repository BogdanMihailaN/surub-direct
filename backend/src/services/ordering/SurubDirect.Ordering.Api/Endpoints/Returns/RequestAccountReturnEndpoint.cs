using System.Security.Claims;
using SurubDirect.Ordering.Application;
using SurubDirect.ServiceDefaults.Security;
namespace SurubDirect.Ordering.Api.Endpoints;
public static class RequestAccountReturnEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPost("/api/v1/ordering/account/orders/{id:guid}/return", async (Guid id, RequestAccountReturnRequest request, HttpContext context, RequestAccountReturnHandler handler, IWebHostEnvironment environment, CancellationToken ct) =>
    {
        context.Response.Headers.CacheControl = "no-store";
        if (!environment.IsDevelopment()) return Results.NotFound();
        var account = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(request.ExpectedAccountId))
            return Results.Problem("Confirmă contul pentru solicitarea returului.", statusCode: 400);
        if (account != request.ExpectedAccountId)
            return Results.Problem("Contul autentificat s-a schimbat. Reîncarcă pagina contului.", statusCode: 409);
        await handler.Handle(account!, id, request.OperationId, request.Reason, ct);
        return Results.NoContent();
    }).WithMetadata(new RemoteSessionRequired()).RequireAuthorization().RequireRateLimiting("cart-storage");
}
