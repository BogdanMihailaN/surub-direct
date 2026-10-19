using Microsoft.Extensions.DependencyInjection;
using SurubDirect.Purchasing.Infrastructure;
using SurubDirect.Purchasing.Application;
using Testcontainers.PostgreSql;
using Xunit;
namespace SurubDirect.Tests;
public sealed class PurchasingFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17").Build();
    public ServiceProvider Services { get; private set; } = null!;
    public async Task InitializeAsync()
    {
        await container.StartAsync();
        var services = new ServiceCollection(); services.AddLogging();
        services.AddPurchasing(container.GetConnectionString());
        services.AddScoped<ICatalogVariants, TestPurchasingCatalog>();
        Services = services.BuildServiceProvider();
        await using var scope = Services.CreateAsyncScope(); await scope.ServiceProvider.MigratePurchasing();
    }
    public async Task DisposeAsync() { await Services.DisposeAsync(); await container.DisposeAsync(); }
}
