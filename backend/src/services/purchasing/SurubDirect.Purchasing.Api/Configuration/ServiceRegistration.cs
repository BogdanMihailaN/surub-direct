using SurubDirect.Purchasing.Application;
using SurubDirect.Purchasing.Infrastructure;
using SurubDirect.Purchasing.Api.Clients;
using SurubDirect.ServiceDefaults.Http;
using SurubDirect.ServiceDefaults.Security;
namespace SurubDirect.Purchasing.Api.Configuration;
public static class ServiceRegistration
{
    public static void AddPurchasingService(this WebApplicationBuilder builder)
    {
        builder.Services.AddPurchasing(builder.Configuration.GetConnectionString("Purchasing") ?? throw new InvalidOperationException("Configure ConnectionStrings__Purchasing."));
        builder.Services.AddScoped<ICatalogVariants, CatalogVariantsClient>();
        builder.Services.AddRemoteSession(builder.Configuration);
        builder.Services.AddServiceHttp("catalog", builder.Configuration["Services:Catalog"] ?? "http://127.0.0.1:5101", ServiceKey.Required(builder.Configuration, "ServiceKeys:Catalog"));
        builder.Services.AddProblemDetails(); builder.Services.AddOpenApi();
    }
}
