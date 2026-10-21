using Microsoft.AspNetCore.Antiforgery;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Api.Security;

namespace SurubDirect.Ordering.Api.Endpoints;
public static class CreateCartSessionEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPost("/api/v1/ordering/cart/session", async (HttpContext context, IAntiforgery antiforgery, CartOwnerCookie owner, GetCartHandler get, CreateCartHandler create, CancellationToken ct) =>
    {
        await antiforgery.ValidateRequestAsync(context);
        context.Response.Headers.CacheControl = "no-store";
        var hash = owner.Read(context);
        if (hash is not null)
        {
            try
            {
                var existing = await get.Handle(hash, ct);
                if (existing is not null)
                    return Results.Ok(existing);
            }
            catch (BusinessException e)when (e.Status == 410)
            {
            }
        }

        var token = owner.NewToken();
        var cart = await create.Handle(owner.Hash(token), ct);
        owner.Write(context, token, cart.ExpiresAt);
        return Results.Ok(cart);
    }).RequireRateLimiting("cart-create");
}
