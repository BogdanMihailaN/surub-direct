using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SurubDirect.ServiceDefaults.Monitoring;
public static class OperationalMonitoringRegistration
{
    public static void AddOperationalMonitor<T>(this IServiceCollection services, IConfiguration configuration)
        where T : OperationalMonitorWorker
    {
        services.AddOptions<OperationalMonitoringOptions>().Bind(configuration.GetSection("Messaging:Monitoring")).ValidateDataAnnotations().ValidateOnStart();
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.AddHostedService<T>();
    }
}
