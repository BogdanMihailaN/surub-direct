using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurubDirect.ServiceDefaults.Monitoring;
using SurubDirect.Inventory.Application;
using SurubDirect.Inventory.Contracts;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Contracts;
using Xunit;
using InventoryWorker = SurubDirect.Inventory.Api.Workers.MessagingMonitorWorker;
using OrderingWorker = SurubDirect.Ordering.Api.Workers.MessagingMonitorWorker;

namespace SurubDirect.Tests;
public sealed class OperationalMonitoringTests
{
    [Fact]
    public void Alerts_emit_only_transitions_and_periodic_reminders()
    {
        var logger = new CapturedLogger<object>();
        var alerts = new OperationalAlerts(logger, TimeSpan.FromMinutes(30));
        var now = DateTimeOffset.UtcNow;
        alerts.Observe("test", false, now);
        alerts.Observe("test", true, now);
        alerts.Observe("test", true, now.AddMinutes(29));
        alerts.Observe("test", null, now.AddMinutes(30));
        Assert.Single(logger.Events);
        alerts.Observe("test", true, now.AddMinutes(30));
        alerts.Observe("test", false, now.AddMinutes(31));
        alerts.Observe("test", false, now.AddMinutes(32));
        alerts.Observe("test", true, now.AddMinutes(33));
        Assert.Equal(new[] { 7100, 7101, 7102, 7100 }, logger.Events.Select(x => x.Id));
    }

    [Fact]
    public async Task Inventory_preserves_active_delay_during_unknown_data_and_recovers()
    {
        var clock = new FixedClock();
        var probe = new OutboxProbe
        {
            Status = new(1, clock.GetUtcNow().AddMinutes(-5), null)
        };
        using var services = new ServiceCollection().AddSingleton<IOutboxMonitor>(probe).BuildServiceProvider();
        var logger = new CapturedLogger<InventoryWorker>();
        using var worker = new InventoryWorker(services.GetRequiredService<IServiceScopeFactory>(), Options.Create(new OperationalMonitoringOptions()), clock, logger);
        await worker.CheckOnce(default);
        Assert.Contains(logger.Events, x => x.Id == 7100 && x.Code == "inventory.outbox-delayed");
        probe.Error = new InvalidOperationException("sensitive connection string");
        await worker.CheckOnce(default);
        Assert.Contains(logger.Events, x => x.Code == "inventory.outbox-unavailable");
        Assert.DoesNotContain(logger.Events, x => x.Id == 7102);
        probe.Error = null;
        probe.Status = new(1, clock.GetUtcNow().AddMinutes(1), null);
        await worker.CheckOnce(default);
        Assert.DoesNotContain(logger.Events, x => x.Id == 7102 && x.Code == "inventory.outbox-delayed");
        probe.Status = new(0, null, null);
        await worker.CheckOnce(default);
        Assert.Contains(logger.Events, x => x.Id == 7102 && x.Code == "inventory.outbox-delayed");
        Assert.DoesNotContain(logger.Events, x => x.Text.Contains("sensitive"));
    }

    [Fact]
    public async Task Ordering_probes_are_independent_and_unknown_broker_counts_do_not_resolve_backlog()
    {
        var probe = new OrderingProbe();
        using var services = new ServiceCollection().AddSingleton<IBrokerMonitor>(probe).AddSingleton<IMessageFailureMonitor>(probe).BuildServiceProvider();
        var logger = new CapturedLogger<OrderingWorker>();
        using var worker = new OrderingWorker(services.GetRequiredService<IServiceScopeFactory>(), Options.Create(new OperationalMonitoringOptions()), new FixedClock(), logger);
        await worker.CheckOnce(default);
        Assert.Contains(logger.Events, x => x.Code == "ordering.ready-backlog");
        probe.Broker = probe.Broker with
        {
            Available = false,
            Ready = null,
            DeadReady = null
        };
        probe.Summary = new(1, 1);
        await worker.CheckOnce(default);
        Assert.Contains(logger.Events, x => x.Code == "ordering.broker-unavailable");
        Assert.Contains(logger.Events, x => x.Code == "ordering.failures-awaiting-review");
        Assert.Contains(logger.Events, x => x.Code == "ordering.replays-awaiting-publication");
        Assert.DoesNotContain(logger.Events, x => x.Id == 7102 && x.Code == "ordering.ready-backlog");
        probe.Broker = probe.Broker with
        {
            Available = true,
            Ready = 99,
            DeadReady = 0
        };
        probe.Summary = new(0, 0);
        await worker.CheckOnce(default);
        Assert.Contains(logger.Events, x => x.Id == 7102 && x.Code == "ordering.ready-backlog");
        Assert.Contains(logger.Events, x => x.Id == 7102 && x.Code == "ordering.replays-awaiting-publication");
    }

    [Fact]
    public async Task Host_cancellation_is_not_reported_as_an_outage()
    {
        var probe = new OutboxProbe
        {
            Error = new OperationCanceledException()
        };
        using var services = new ServiceCollection().AddSingleton<IOutboxMonitor>(probe).BuildServiceProvider();
        var logger = new CapturedLogger<InventoryWorker>();
        using var worker = new InventoryWorker(services.GetRequiredService<IServiceScopeFactory>(), Options.Create(new OperationalMonitoringOptions()), new FixedClock(), logger);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => worker.CheckOnce(new CancellationToken(true)));
        Assert.Empty(logger.Events);
    }

    [Fact]
    public async Task Disabled_monitor_does_not_probe()
    {
        var probe = new OutboxProbe
        {
            Error = new InvalidOperationException()
        };
        using var services = new ServiceCollection().AddSingleton<IOutboxMonitor>(probe).BuildServiceProvider();
        var logger = new CapturedLogger<InventoryWorker>();
        using var worker = new InventoryWorker(services.GetRequiredService<IServiceScopeFactory>(), Options.Create(new OperationalMonitoringOptions { Enabled = false }), new FixedClock(), logger);
        await worker.StartAsync(default);
        await worker.StopAsync(default);
        Assert.Empty(logger.Events);
        Assert.Equal(0, probe.Calls);
    }

    [Fact]
    public async Task Probe_timeout_is_reported_and_next_observation_can_recover()
    {
        var probe = new OutboxProbe
        {
            Hang = true
        };
        using var services = new ServiceCollection().AddSingleton<IOutboxMonitor>(probe).BuildServiceProvider();
        var logger = new CapturedLogger<InventoryWorker>();
        using var worker = new InventoryWorker(services.GetRequiredService<IServiceScopeFactory>(), Options.Create(new OperationalMonitoringOptions { ProbeTimeoutSeconds = 1 }), new FixedClock(), logger);
        await worker.CheckOnce(default);
        Assert.Contains(logger.Events, x => x.Id == 7100 && x.Code == "inventory.outbox-unavailable");
        probe.Hang = false;
        await worker.CheckOnce(default);
        Assert.Contains(logger.Events, x => x.Id == 7102 && x.Code == "inventory.outbox-unavailable");
    }

    [Theory]
    [InlineData("IntervalSeconds", "0")]
    [InlineData("ProbeTimeoutSeconds", "0")]
    [InlineData("ReminderSeconds", "0")]
    public void Invalid_monitor_intervals_are_rejected(string key, string value)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Messaging:Monitoring:" + key] = value }).Build();
        var services = new ServiceCollection();
        services.AddOperationalMonitor<InventoryWorker>(configuration);
        using var provider = services.BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<OperationalMonitoringOptions>>().Value);
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.Parse("2026-10-23T10:00:00Z");
    }

    private sealed class OutboxProbe : IOutboxMonitor
    {
        public OutboxStatusDto Status = new(0, null, null);
        public Exception? Error;
        public int Calls;
        public bool Hang;
        public async Task<OutboxStatusDto> Read(CancellationToken ct)
        {
            Calls++;
            if (Hang)
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            if (Error is not null)
                throw Error;
            return Status;
        }
    }

    private sealed class OrderingProbe : IBrokerMonitor, IMessageFailureMonitor
    {
        public BrokerStatusDto Broker = new(true, true, 100, 1, DateTimeOffset.UtcNow);
        public MessageFailureSummary Summary = new(0, 0);
        Task<BrokerStatusDto> IBrokerMonitor.Read(CancellationToken ct) => Task.FromResult(Broker);
        Task<MessageFailureSummary> IMessageFailureMonitor.Read(CancellationToken ct) => Task.FromResult(Summary);
    }

    private sealed class CapturedLogger<T> : ILogger<T>
    {
        public List<(int Id, string Code, string Text)> Events { get; } = new();

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var values = (IEnumerable<KeyValuePair<string, object?>>)(object)state!;
            Events.Add((id.Id, (string)values.Single(x => x.Key == "AlertCode").Value!, formatter(state, exception)));
        }
    }
}
