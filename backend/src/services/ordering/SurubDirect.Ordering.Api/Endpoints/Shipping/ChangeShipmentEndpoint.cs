using System.Security.Claims;
using SurubDirect.Ordering.Application;
namespace SurubDirect.Ordering.Api.Endpoints;
public static class ChangeShipmentEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPost("/api/v1/admin/sales-orders/{id:guid}/shipment", async (Guid id, ChangeShipmentRequest request, HttpContext context, ChangeShipmentHandler handler, IWebHostEnvironment environment, CancellationToken ct) =>
    {
        context.Response.Headers.CacheControl = "no-store";
        if (!environment.IsDevelopment()) return Results.NotFound();
        var actor = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actor)) return Results.Unauthorized();
        await handler.Handle(id, request.OperationId, request.ExpectedVersion, request.Target, request.Reason, actor, ct);
        return Results.NoContent();
    }).RequireAuthorization("Admin");
}
