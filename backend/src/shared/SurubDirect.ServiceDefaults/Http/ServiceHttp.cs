using Microsoft.Extensions.DependencyInjection;

namespace SurubDirect.ServiceDefaults.Http;
public static class ServiceHttp
{
    public static IHttpClientBuilder AddServiceHttp(this IServiceCollection services, string name, string address, string key)
    {
        var uri = new Uri(address, UriKind.Absolute);
        return services.AddHttpClient(name, client =>
        {
            client.BaseAddress = uri;
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.Add("X-Service-Key", key);
        }).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { UseCookies = false, AllowAutoRedirect = false });
    }
}
