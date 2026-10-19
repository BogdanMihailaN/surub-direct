using SurubDirect.Purchasing.Application;
namespace SurubDirect.Purchasing.Api.Endpoints;
public static class UpdateSupplierItemEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPut("/suppliers/{supplierId:guid}/items/{id:guid}", async (Guid supplierId, Guid id, UpdateSupplierItem command, UpdateSupplierItemHandler handler, CancellationToken ct) =>
        { await handler.Handle(supplierId, id, command, ct); return Results.NoContent(); });
}
