using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Api.Security;
using SurubDirect.ServiceDefaults.Security;
namespace SurubDirect.Ordering.Api.Endpoints;
public static class SaveAccountCartCustomerEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPut("/api/v1/ordering/account/cart/customer", async (SaveCartCustomer command, string expectedAccountId, HttpContext context, SaveAccountCartCustomerHandler handler, IWebHostEnvironment environment, CancellationToken ct) =>
    {
        context.Response.Headers.CacheControl = "no-store";
        if (!environment.IsDevelopment()) return Results.NotFound();
        var accountId = AccountCartIdentity.Require(context, expectedAccountId);
        var cart = await handler.Handle(accountId, command, ct);
        return Results.Ok(cart);
    }).WithMetadata(new RemoteSessionRequired()).RequireAuthorization().RequireRateLimiting("cart-storage");
}
