using SurubDirect.Inventory.Application;
using SurubDirect.ServiceDefaults.Security;
namespace SurubDirect.Inventory.Api.Endpoints;

public static class CorrectReceiptEndpoint
{
    public static void Map(IEndpointRouteBuilder routes, IConfiguration configuration)
    {
        var key = ServiceKey.Required(configuration, "ServiceKeys:Inventory");
        routes.MapPost("/internal/receipt-corrections", async (CorrectReceipt c, CorrectReceiptHandler handler, HttpContext context, CancellationToken ct) =>
        {
            if (!ServiceKey.Matches(context.Request, key)) return Results.Unauthorized();
            return Results.Ok(new { created = await handler.Handle(c, context.User.Identity!.Name!, ct) });
        }).WithMetadata(new RemoteSessionRequired()).RequireAuthorization("Admin");
    }
}
