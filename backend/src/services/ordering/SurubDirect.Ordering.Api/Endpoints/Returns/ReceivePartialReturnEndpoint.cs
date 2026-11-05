using System.Security.Claims;
using SurubDirect.Ordering.Application;
namespace SurubDirect.Ordering.Api.Endpoints;
public static class ReceivePartialReturnEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPost("/api/v1/admin/sales-orders/{id:guid}/partial-returns/{requestId:guid}/receive", async (Guid id, Guid requestId, ReceivePartialReturnRequest request, HttpContext context, ReceivePartialReturnHandler handler, IWebHostEnvironment environment, CancellationToken ct) =>
    {
        context.Response.Headers.CacheControl = "no-store";
        if (!environment.IsDevelopment()) return Results.NotFound();
        var actor = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actor)) return Results.Unauthorized();
        await handler.Handle(id, requestId, request.OperationId, request.ExpectedVersion, request.Reason, actor, request.ResalableConfirmed, ct);
        return Results.NoContent();
    }).RequireAuthorization("Admin");
}
