using Npgsql;

namespace SurubDirect.Ordering.Api.Endpoints;
public static class ReadinessEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapGet("/health/ready", async (NpgsqlDataSource source, CancellationToken ct) =>
    {
        try
        {
            await using var connection = await source.OpenConnectionAsync(ct);
            return Results.Ok();
        }
        catch (NpgsqlException)
        {
            return Results.StatusCode(503);
        }
    });
}
