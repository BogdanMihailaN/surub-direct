using SurubDirect.Catalog.Infrastructure;
using SurubDirect.Pricing.Infrastructure;

namespace SurubDirect.Catalog.Api.Bootstrap;
public static class DatabaseInitialization
{
    public static async Task InitializeCatalog(this WebApplication app, bool seed)
    {
        if (!app.Environment.IsDevelopment())
            throw new InvalidOperationException("Use controlled migrations outside Development.");
        await using var scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.MigrateCatalog();
        await scope.ServiceProvider.MigratePricing();
        if (seed)
            await DemoData.Seed(scope.ServiceProvider);
    }
}
