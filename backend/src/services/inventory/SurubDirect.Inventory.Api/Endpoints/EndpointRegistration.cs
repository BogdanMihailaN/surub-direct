namespace SurubDirect.Inventory.Api.Endpoints;
public static class EndpointRegistration
{
    public static void MapInventoryEndpoints(this WebApplication app)
    {
        ReceiveStockEndpoint.Map(app.MapGroup("/api/v1/admin").RequireAuthorization("Admin"));
        StockQueryEndpoint.Map(app, app.Configuration);
        LivenessEndpoint.Map(app);
        ReadinessEndpoint.Map(app);
    }
}
