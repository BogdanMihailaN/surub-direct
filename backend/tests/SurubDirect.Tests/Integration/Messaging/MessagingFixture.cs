using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SurubDirect.Messaging;
using SurubDirect.Inventory.Infrastructure;
using SurubDirect.Ordering.Infrastructure;
using SurubDirect.Ordering.Application;
using Testcontainers.PostgreSql;
using Xunit;

namespace SurubDirect.Tests;
public sealed class MessagingFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:17").Build();
    private readonly string password = Guid.NewGuid().ToString("N");
    private IContainer rabbit = null!;
    public ServiceProvider Services { get; private set; } = null!;
    public BrokerSettings Settings { get; private set; } = null!;
    public FakeReservationInventory Remote { get; } = new();

    public async Task InitializeAsync()
    {
        rabbit = new ContainerBuilder("rabbitmq:4.2-management").WithEnvironment("RABBITMQ_DEFAULT_USER", "test").WithEnvironment("RABBITMQ_DEFAULT_PASS", password).WithPortBinding(5672, true).WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Server startup complete")).Build();
        await Task.WhenAll(postgres.StartAsync(), rabbit.StartAsync());
        Settings = new(rabbit.Hostname, rabbit.GetMappedPublicPort(5672), "/", "test", password);
        await using (var connection = new NpgsqlConnection(postgres.GetConnectionString()))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("CREATE DATABASE ordering_tests", connection);
            await command.ExecuteNonQueryAsync();
        }

        var ordering = new NpgsqlConnectionStringBuilder(postgres.GetConnectionString())
        {
            Database = "ordering_tests"
        }.ConnectionString;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInventory(postgres.GetConnectionString());
        services.AddOrderingPersistence(ordering);
        services.AddSingleton<IReservationInventory>(Remote);
        Services = services.BuildServiceProvider();
        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.MigrateInventory();
        await scope.ServiceProvider.MigrateOrdering();
    }

    public async Task DisposeAsync()
    {
        await Services.DisposeAsync();
        await rabbit.DisposeAsync();
        await postgres.DisposeAsync();
    }
}
