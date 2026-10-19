namespace SurubDirect.Purchasing.Api.Endpoints;
public static class EndpointRegistration
{
    public static void MapPurchasingEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/api/v1/admin/purchasing").RequireAuthorization("Admin");
        CreateSupplierEndpoint.Map(admin);
        UpdateSupplierEndpoint.Map(admin);
        GetSupplierEndpoint.Map(admin);
        ListSuppliersEndpoint.Map(admin);
        AddSupplierItemEndpoint.Map(admin);
        UpdateSupplierItemEndpoint.Map(admin);
        SearchVariantsEndpoint.Map(admin);
        LivenessEndpoint.Map(app); ReadinessEndpoint.Map(app);
    }
}
