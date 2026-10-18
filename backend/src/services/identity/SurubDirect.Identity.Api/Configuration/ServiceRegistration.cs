using Microsoft.AspNetCore.RateLimiting;
using SurubDirect.Identity.Infrastructure;
using SurubDirect.ServiceDefaults.Security;

namespace SurubDirect.Identity.Api.Configuration;
public static class ServiceRegistration
{
    public static void AddIdentityService(this WebApplicationBuilder builder)
    {
        builder.Services.AddStoreIdentity(builder.Configuration.GetConnectionString("Identity") ?? throw new InvalidOperationException("Configure ConnectionStrings__Identity."));
        ServiceKey.Required(builder.Configuration, "ServiceKeys:Identity");
        builder.Services.ConfigureApplicationCookie(o =>
        {
            o.Cookie.Name = "SurubDirect.Session";
            o.Cookie.HttpOnly = true;
            o.Cookie.SameSite = SameSiteMode.Lax;
            o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            o.Events.OnRedirectToLogin = c =>
            {
                c.Response.StatusCode = 401;
                return Task.CompletedTask;
            };
            o.Events.OnRedirectToAccessDenied = c =>
            {
                c.Response.StatusCode = 403;
                return Task.CompletedTask;
            };
        });
        builder.Services.AddAuthorization();
        builder.Services.AddAntiforgery(o =>
        {
            o.HeaderName = "X-CSRF-TOKEN";
            o.Cookie.Name = "SurubDirect.Antiforgery";
            o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        });
        builder.Services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = 429;
            o.AddFixedWindowLimiter("login", x =>
            {
                x.PermitLimit = 20;
                x.Window = TimeSpan.FromMinutes(1);
                x.QueueLimit = 0;
            });
        });
        builder.Services.AddProblemDetails();
        builder.Services.AddOpenApi();
    }
}
