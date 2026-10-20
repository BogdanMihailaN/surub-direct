namespace SurubDirect.Inventory.Api.Endpoints;

public static class EndpointRegistration
{
    public static void MapInventoryEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/api/v1/admin").RequireAuthorization("Admin");
        ReceiveStockEndpoint.Map(admin);
        AdjustStockEndpoint.Map(admin);
        GetStockLedgerEndpoint.Map(admin);
        StockQueryEndpoint.Map(app, app.Configuration);
        CorrectReceiptEndpoint.Map(app, app.Configuration);
        LivenessEndpoint.Map(app);
        ReadinessEndpoint.Map(app);
    }
}
