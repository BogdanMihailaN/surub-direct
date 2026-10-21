using SurubDirect.Ordering.Api.Security;
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
        builder.Services.AddOrderingPersistence(builder.Configuration.GetConnectionString("Ordering")!);
        builder.Services.AddScoped<CartOwnerCookie>();
        builder.Services.AddAntiforgery(o =>
        {
            o.HeaderName = "X-CART-CSRF";
            o.Cookie.Name = "SurubDirect.CartCsrf";
            o.Cookie.Path = "/api/v1/ordering";
            o.Cookie.SameSite = SameSiteMode.Strict;
            o.Cookie.HttpOnly = true;
            o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        });
        builder.Services.AddScoped<QuoteCartHandler>();
        builder.Services.AddScoped<ICartCatalog, CartCatalogClient>();
        builder.Services.AddHttpClient("cart-catalog", c =>
        {
            c.BaseAddress = new Uri(builder.Configuration["Services:Catalog"] ?? "http://127.0.0.1:5101");
            c.Timeout = TimeSpan.FromSeconds(5);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false, AllowAutoRedirect = false });
        builder.Services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = 429;
            o.AddPolicy("cart-storage", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            o.AddPolicy("cart-create", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            o.AddPolicy("cart-quotes", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
    }
}
