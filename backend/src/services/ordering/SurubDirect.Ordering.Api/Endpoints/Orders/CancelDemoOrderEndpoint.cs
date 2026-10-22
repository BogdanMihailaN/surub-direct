using System.Security.Claims;
using SurubDirect.Ordering.Application;

namespace SurubDirect.Ordering.Api.Endpoints;
public static class CancelDemoOrderEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPost("/api/v1/admin/sales-orders/{id:guid}/cancel", async (Guid id, CancelDemoOrderRequest request, HttpContext context, CancelDemoOrderHandler handler, IWebHostEnvironment environment, CancellationToken ct) =>
    {
        context.Response.Headers.CacheControl = "no-store";
        if (!environment.IsDevelopment())
            return Results.NotFound();
        var actor = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actor))
            return Results.Unauthorized();
        return Results.Ok(await handler.Handle(id, request.Reason, actor, ct));
    }).RequireAuthorization("Admin");
}
