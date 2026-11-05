using SurubDirect.Inventory.Application;
using SurubDirect.ServiceDefaults.Security;
namespace SurubDirect.Inventory.Api.Endpoints;
public static class ReceiveReturnEndpoint
{
    public static void Map(IEndpointRouteBuilder routes, IConfiguration configuration)
    {
        var key = ServiceKey.Required(configuration, "ServiceKeys:Reservations");
        routes.MapPost("/internal/reservations/{id:guid}/return-receipts/{receiptId:guid}", async (Guid id, Guid receiptId, ReceiveReturn command, HttpContext context, ReceiveReturnHandler handler, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            if (!ServiceKey.Matches(context.Request, key)) return Results.Unauthorized();
            return Results.Ok(await handler.Handle(id, receiptId, command, ct));
        });
    }
}
