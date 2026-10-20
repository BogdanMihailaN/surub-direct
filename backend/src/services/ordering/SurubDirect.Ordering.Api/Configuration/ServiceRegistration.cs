using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Npgsql;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Infrastructure;
namespace SurubDirect.Ordering.Api.Configuration;

public static class ServiceRegistration
{
    public static void AddOrdering(this WebApplicationBuilder builder)
    {
        builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 65536);
        builder.Services.AddSingleton(NpgsqlDataSource.Create(builder.Configuration.GetConnectionString("Ordering") ?? throw new InvalidOperationException("Configure ConnectionStrings__Ordering.")));
        builder.Services.AddScoped<QuoteCartHandler>(); builder.Services.AddScoped<ICartCatalog, CartCatalogClient>();
        builder.Services.AddHttpClient("cart-catalog", c => { c.BaseAddress = new Uri(builder.Configuration["Services:Catalog"] ?? "http://127.0.0.1:5101"); c.Timeout = TimeSpan.FromSeconds(5); }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false, AllowAutoRedirect = false });
        builder.Services.AddRateLimiter(o => { o.RejectionStatusCode = 429; o.AddPolicy("cart-quotes", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })); });
    }
}
