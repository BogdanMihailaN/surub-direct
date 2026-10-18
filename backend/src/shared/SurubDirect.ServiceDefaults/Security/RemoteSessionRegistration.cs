using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.ServiceDefaults.Http;

namespace SurubDirect.ServiceDefaults.Security;
public static class RemoteSessionRegistration
{
    public static IServiceCollection AddRemoteSession(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddServiceHttp("identity", configuration["Services:Identity"] ?? "http://127.0.0.1:5103", ServiceKey.Required(configuration, "ServiceKeys:Identity"));
        services.AddAuthentication("RemoteSession").AddScheme<AuthenticationSchemeOptions, RemoteSessionHandler>("RemoteSession", _ =>
        {
        });
        services.AddAuthorization(options => options.AddPolicy("Admin", policy => policy.RequireRole("Administrator")));
        return services;
    }
}
