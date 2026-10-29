using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Api.Security;
using SurubDirect.ServiceDefaults.Security;
namespace SurubDirect.Ordering.Api.Endpoints;
public static class ImportGuestCartEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPost("/api/v1/ordering/account/cart/import", async (ImportGuestCart command, CartOwnerCookie owner, string expectedAccountId, HttpContext context, ImportGuestCartHandler handler, IWebHostEnvironment environment, CancellationToken ct) =>
    {
        context.Response.Headers.CacheControl = "no-store";
        if (!environment.IsDevelopment()) return Results.NotFound();
        var accountId = AccountCartIdentity.Require(context, expectedAccountId);
        var cart = await handler.Handle(accountId, owner.Require(context), command, ct);
        return Results.Ok(cart);
    }).WithMetadata(new RemoteSessionRequired()).RequireAuthorization().RequireRateLimiting("cart-storage");
}
