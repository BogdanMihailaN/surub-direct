using SurubDirect.Catalog.Infrastructure;
using SurubDirect.Store.Infrastructure;
using SurubDirect.Pricing.Infrastructure;
using SurubDirect.Store.Application;
using SurubDirect.Catalog.Api.Clients;
using SurubDirect.ServiceDefaults.Http;
using SurubDirect.ServiceDefaults.Security;

namespace SurubDirect.Catalog.Api.Configuration;
public static class ServiceRegistration
{
    public static void AddCatalogService(this WebApplicationBuilder builder)
    {
        var connection = builder.Configuration.GetConnectionString("Catalog") ?? throw new InvalidOperationException("Configure ConnectionStrings__Catalog.");
        builder.Services.AddCatalog(connection).AddPricing(connection);
        var mediaRoot = builder.Configuration["Media:Root"] ?? throw new InvalidOperationException("Configure Media__Root to a private persistent directory.");
        builder.Services.AddCatalogMedia(mediaRoot);
        builder.Services.AddCatalogImports(connection);
        builder.Services.AddScoped<ProductEnricher>();
        builder.Services.AddScoped<GetCatalogHandler>();
        builder.Services.AddScoped<GetProductHandler>();
        builder.Services.AddScoped<GetAdminProductHandler>();
        builder.Services.AddScoped<PublishStoreProductHandler>();
        builder.Services.AddScoped<IStockReader, InventoryClient>();
        builder.Services.AddRemoteSession(builder.Configuration);
        builder.Services.AddServiceHttp("inventory", builder.Configuration["Services:Inventory"] ?? "http://127.0.0.1:5102", ServiceKey.Required(builder.Configuration, "ServiceKeys:Inventory"));
        ServiceKey.Required(builder.Configuration, "ServiceKeys:Catalog");
        builder.Services.AddProblemDetails();
        builder.Services.AddOpenApi();
    }
}
