using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Api.Security;
using SurubDirect.ServiceDefaults.Security;
namespace SurubDirect.Ordering.Api.Endpoints;
public static class CreateAccountCartEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPost("/api/v1/ordering/account/cart/session", async (string expectedAccountId, HttpContext context, CreateAccountCartHandler handler, IWebHostEnvironment environment, CancellationToken ct) =>
    {
        context.Response.Headers.CacheControl = "no-store";
        if (!environment.IsDevelopment()) return Results.NotFound();
        var accountId = AccountCartIdentity.Require(context, expectedAccountId);
        var cart = await handler.Handle(accountId, ct);
        return Results.Ok(cart);
    }).WithMetadata(new RemoteSessionRequired()).RequireAuthorization().RequireRateLimiting("cart-create");
}
