using SurubDirect.Ordering.Application;

namespace SurubDirect.Ordering.Api.Endpoints;
public static class ListAdminOrdersEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapGet("/api/v1/admin/sales-orders", async (string? status, string? number, int? page, HttpContext context, ListAdminOrdersHandler handler, IWebHostEnvironment environment, CancellationToken ct) =>
    {
        context.Response.Headers.CacheControl = "no-store";
        if (!environment.IsDevelopment())
            return Results.NotFound();
        return Results.Ok(await handler.Handle(status, number, page ?? 1, ct));
    }).RequireAuthorization("Admin");
}
