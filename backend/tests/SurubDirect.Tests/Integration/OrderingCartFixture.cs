using Microsoft.Extensions.DependencyInjection;
using SurubDirect.Ordering.Infrastructure;
using Testcontainers.PostgreSql;
using Xunit;

namespace SurubDirect.Tests;
public sealed class OrderingCartFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17").Build();
    public ServiceProvider Services { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await container.StartAsync();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOrderingPersistence(container.GetConnectionString());
        Services = services.BuildServiceProvider();
        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.MigrateOrdering();
    }

    public async Task DisposeAsync()
    {
        await Services.DisposeAsync();
        await container.DisposeAsync();
    }
}
