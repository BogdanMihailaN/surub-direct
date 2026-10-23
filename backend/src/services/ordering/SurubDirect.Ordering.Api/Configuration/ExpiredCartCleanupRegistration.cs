using SurubDirect.Ordering.Api.Workers;

namespace SurubDirect.Ordering.Api.Configuration;
public static class ExpiredCartCleanupRegistration
{
    public static void AddExpiredCartCleanup(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<ExpiredCartCleanupOptions>().Bind(builder.Configuration.GetSection("CartCleanup")).ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddHostedService<ExpiredCartCleanupWorker>();
    }
}
