namespace SurubDirect.Inventory.Api.Endpoints;
public static class EndpointRegistration
{
    public static void MapInventoryEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/api/v1/admin").RequireAuthorization("Admin");
        GetOutboxStatusEndpoint.Map(admin);
        ReceiveStockEndpoint.Map(admin);
        AdjustStockEndpoint.Map(admin);
        GetStockLedgerEndpoint.Map(admin);
        StockQueryEndpoint.Map(app, app.Configuration);
        CorrectReceiptEndpoint.Map(app, app.Configuration);
        ReserveStockEndpoint.Map(app, app.Configuration);
        ConsumeStockEndpoint.Map(app, app.Configuration);
        RestoreStockEndpoint.Map(app, app.Configuration);
        ReleaseStockEndpoint.Map(app, app.Configuration);
        GetReservationEndpoint.Map(app, app.Configuration);
        LivenessEndpoint.Map(app);
        ReadinessEndpoint.Map(app);
    }
}
