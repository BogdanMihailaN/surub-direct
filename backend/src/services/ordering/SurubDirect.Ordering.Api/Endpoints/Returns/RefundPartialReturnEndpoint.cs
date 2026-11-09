using System.Security.Claims;
using SurubDirect.Ordering.Application;
namespace SurubDirect.Ordering.Api.Endpoints;
public static class RefundPartialReturnEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPost("/api/v1/admin/sales-orders/{id:guid}/partial-returns/{requestId:guid}/refund", async (Guid id, Guid requestId, RefundPartialReturnRequest request, HttpContext context, RefundPartialReturnHandler handler, IWebHostEnvironment environment, CancellationToken ct) =>
    {
        context.Response.Headers.CacheControl = "no-store";
        if (!environment.IsDevelopment()) return Results.NotFound();
        var actor = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actor)) return Results.Unauthorized();
        await handler.Handle(id, requestId, request.OperationId, request.ExpectedVersion, request.Reason, actor, ct);
        return Results.NoContent();
    }).RequireAuthorization("Admin");
}
