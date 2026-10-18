namespace SurubDirect.Identity.Api.Endpoints;
public static class LivenessEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapGet("/health/live", () => Results.Ok(new { service = "identity" }));
}
