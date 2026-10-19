using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Npgsql;
using SurubDirect.Inventory.Application;
using Xunit;
using SurubDirect.Catalog.Infrastructure;
using SurubDirect.Pricing.Infrastructure;
using SurubDirect.Inventory.Infrastructure;
using SurubDirect.Store.Application;
using SurubDirect.Store.Infrastructure;

namespace SurubDirect.Tests;
public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17").Build();
    private readonly string mediaRoot = Path.Combine(Path.GetTempPath(), "surub-images-" + Guid.NewGuid().ToString("N"));
    public ServiceProvider Services { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await container.StartAsync();
        var s = new ServiceCollection();
        await using (var connection = new NpgsqlConnection(container.GetConnectionString()))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("CREATE DATABASE inventory_tests", connection);
            await command.ExecuteNonQueryAsync();
        }

        var inventoryConnection = new NpgsqlConnectionStringBuilder(container.GetConnectionString())
        {
            Database = "inventory_tests"
        }.ConnectionString;
        s.AddLogging();
        s.AddCatalogImports(container.GetConnectionString());
        s.AddCatalogMedia(mediaRoot);
        s.AddCatalog(container.GetConnectionString()).AddPricing(container.GetConnectionString()).AddInventory(inventoryConnection);
        s.AddScoped<IVariantLookup, TestVariantLookup>();
        s.AddScoped<IStockReader, TestStockReader>();
        s.AddScoped<ProductEnricher>();
        s.AddScoped<GetCatalogHandler>();
        s.AddScoped<GetProductHandler>();
        s.AddScoped<PublishStoreProductHandler>();
        Services = s.BuildServiceProvider();
        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.MigrateCatalog();
        await scope.ServiceProvider.MigratePricing();
        await scope.ServiceProvider.MigrateInventory();
    }

    public async Task DisposeAsync()
    {
        await Services.DisposeAsync();
        await container.DisposeAsync();
        if (Directory.Exists(mediaRoot)) Directory.Delete(mediaRoot, true);
    }
}
