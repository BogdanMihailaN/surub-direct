using SurubDirect.Purchasing.Application;
namespace SurubDirect.Purchasing.Api.Endpoints;
public static class CreateSupplierEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPost("/suppliers", async (CreateSupplier command, CreateSupplierHandler handler, CancellationToken ct) =>
        { var id = await handler.Handle(command, ct); return Results.Created($"/api/v1/admin/purchasing/suppliers/{id}", new { id }); });
}
