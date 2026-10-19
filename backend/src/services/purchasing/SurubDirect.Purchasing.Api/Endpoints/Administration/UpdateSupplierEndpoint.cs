using SurubDirect.Purchasing.Application;
namespace SurubDirect.Purchasing.Api.Endpoints;
public static class UpdateSupplierEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPut("/suppliers/{id:guid}", async (Guid id, UpdateSupplier command, UpdateSupplierHandler handler, CancellationToken ct) =>
        { await handler.Handle(id, command, ct); return Results.NoContent(); });
}
