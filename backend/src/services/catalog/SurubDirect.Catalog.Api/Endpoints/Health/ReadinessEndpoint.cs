using Microsoft.EntityFrameworkCore;
using SurubDirect.Catalog.Infrastructure;

namespace SurubDirect.Catalog.Api.Endpoints;
public static class ReadinessEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapGet("/health/ready", async (CatalogDbContext db, CancellationToken ct) => await db.Database.CanConnectAsync(ct) ? Results.Ok() : Results.StatusCode(503));
}
