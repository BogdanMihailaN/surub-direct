using Microsoft.EntityFrameworkCore;
using SurubDirect.Purchasing.Infrastructure;

namespace SurubDirect.Purchasing.Api.Endpoints;
public static class ReadinessEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapGet("/health/ready", async (PurchasingDbContext db, CancellationToken ct) => await db.Database.CanConnectAsync(ct) ? Results.Ok() : Results.StatusCode(503));
}
