using Microsoft.EntityFrameworkCore;
using SurubDirect.Identity.Infrastructure;

namespace SurubDirect.Identity.Api.Endpoints;
public static class ReadinessEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapGet("/health/ready", async (StoreIdentityDbContext db, CancellationToken ct) => await db.Database.CanConnectAsync(ct) ? Results.Ok() : Results.StatusCode(503));
}
