using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Contracts;
using SurubDirect.Ordering.Api.Security;

namespace SurubDirect.Ordering.Api.Endpoints;
public static class GetDemoOrdersEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapGet("/api/v1/ordering/demo-orders", async (int? page, HttpContext context, CartOwnerCookie owner, GetDemoOrdersHandler handler, IWebHostEnvironment environment, CancellationToken ct) =>
    {
        if (!environment.IsDevelopment())
            return Results.NotFound();
        context.Response.Headers.CacheControl = "no-store";
        var identity = owner.Read(context);
        return Results.Ok(identity is null ? new DemoOrderPageDto([], 1, 0, 25) : await handler.Handle(identity, page ?? 1, ct));
    }).RequireRateLimiting("cart-storage");
}
