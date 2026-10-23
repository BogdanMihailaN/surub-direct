using SurubDirect.Inventory.Api.Workers;
using SurubDirect.Inventory.Application;
using SurubDirect.Inventory.Infrastructure;
using SurubDirect.Inventory.Api.Clients;
using SurubDirect.ServiceDefaults.Http;
using SurubDirect.ServiceDefaults.Security;

namespace SurubDirect.Inventory.Api.Configuration;
public static class ServiceRegistration
{
    public static void AddInventoryService(this WebApplicationBuilder builder)
    {
        builder.Services.AddInventory(builder.Configuration.GetConnectionString("Inventory") ?? throw new InvalidOperationException("Configure ConnectionStrings__Inventory."));
        builder.Services.AddScoped<IVariantLookup, CatalogClient>();
        builder.Services.AddRemoteSession(builder.Configuration);
        builder.Services.AddServiceHttp("catalog", builder.Configuration["Services:Catalog"] ?? "http://127.0.0.1:5101", ServiceKey.Required(builder.Configuration, "ServiceKeys:Catalog"));
        ServiceKey.Required(builder.Configuration, "ServiceKeys:Inventory");
        ServiceKey.Required(builder.Configuration, "ServiceKeys:Reservations");
        builder.Services.AddHostedService<ReservationExpiryWorker>();
        builder.AddReservationMessaging();
        builder.Services.AddProblemDetails();
        builder.Services.AddOpenApi();
    }
}
