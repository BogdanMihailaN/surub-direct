using SurubDirect.Purchasing.Application;
namespace SurubDirect.Purchasing.Api.Endpoints;
public static class AddSupplierItemEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPost("/suppliers/{supplierId:guid}/items", async (Guid supplierId, AddSupplierItem command, AddSupplierItemHandler handler, CancellationToken ct) =>
        { var id = await handler.Handle(supplierId, command, ct); return Results.Created($"/api/v1/admin/purchasing/suppliers/{supplierId}/items/{id}", new { id }); });
}
