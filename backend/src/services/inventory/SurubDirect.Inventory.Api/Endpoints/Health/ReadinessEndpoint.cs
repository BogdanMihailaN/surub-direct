using Microsoft.EntityFrameworkCore;
using SurubDirect.Inventory.Infrastructure;

namespace SurubDirect.Inventory.Api.Endpoints;
public static class ReadinessEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapGet("/health/ready", async (InventoryDbContext db, CancellationToken ct) => await db.Database.CanConnectAsync(ct) ? Results.Ok() : Results.StatusCode(503));
}
