using SurubDirect.Ordering.Application;
namespace SurubDirect.Ordering.Api.Endpoints;
public static class ListPartialReturnsEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapGet("/api/v1/admin/sales-orders/{id:guid}/partial-returns", async (Guid id, HttpContext context, ListPartialReturnsHandler handler, IWebHostEnvironment environment, CancellationToken ct) =>
    {
        context.Response.Headers.CacheControl = "no-store";
        if (!environment.IsDevelopment()) return Results.NotFound();
        return Results.Ok(await handler.Handle(id, ct));
    }).RequireAuthorization("Admin");
}
