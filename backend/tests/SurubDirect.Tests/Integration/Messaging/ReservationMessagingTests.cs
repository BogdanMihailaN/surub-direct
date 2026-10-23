using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.Messaging;
using SurubDirect.Inventory.Application;
using SurubDirect.Inventory.Domain;
using SurubDirect.Inventory.Infrastructure;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Infrastructure;
using Xunit;
using Outcome = SurubDirect.Ordering.Infrastructure.ReservationOutcome;
using OutcomeLine = SurubDirect.Ordering.Infrastructure.ReservationOutcomeLine;

namespace SurubDirect.Tests;
public sealed class ReservationMessagingTests(MessagingFixture fixture) : IClassFixture<MessagingFixture>
{
    private static BrokerTopology Topology()
    {
        var id = Guid.NewGuid().ToString("N");
        return new("test." + id, "queue." + id, "outcome", "dead." + id);
    }

    private async Task<(string Owner, Guid Id)> Seed()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var s = scope.ServiceProvider;
        var owner = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var carts = s.GetRequiredService<IShoppingCartRepository>();
        var cart = await carts.Create(owner, default);
        var catalog = new PreparationCatalog();
        cart = await carts.Replace(owner, cart.Id, cart.Version, [new("surub", catalog.First, 2), new("surub", catalog.Second, 3)], default);
        cart = await carts.SaveCustomer(owner, cart.Id, cart.Version, CustomerDraftSamples.Person(), default);
        var id = Guid.NewGuid();
        await new PrepareCartHandler(carts, s.GetRequiredService<IPreparedCarts>(), new(catalog)).Handle(owner, new(id, cart.Id, cart.Version), default);
        await s.GetRequiredService<ProcessReservationPlanHandler>().Handle(default);
        await s.GetRequiredService<IDemoOrders>().Confirm(owner, id, default);
        var plan = (await s.GetRequiredService<IReservationPlans>().Read(id, default))!;
        foreach (var line in plan.Lines)
            await s.GetRequiredService<IInventoryRepository>().Receive(StockReceipt.Create(Guid.NewGuid(), line.VariantId, 100, "TEST", "test"), default);
        var reservations = s.GetRequiredService<IStockReservations>();
        await reservations.Reserve(StockReservation.Create(id, id, 900, plan.Lines.Select(x => new ReservationLine(x.VariantId, x.Quantity)).ToArray(), DateTimeOffset.UtcNow), default);
        await reservations.Consume(id, id, default);
        return (owner, id);
    }

    private async Task Handle(BrokerDelivery d)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ReservationInbox>().Handle(d.MessageId, d.Type, d.Body, default);
    }

    private static async Task<BrokerDelivery> Read(RabbitSession session, bool dead = false)
    {
        for (var i = 0; i < 100; i++)
        {
            var d = await session.Read(default, dead);
            if (d is not null)
                return d;
            await Task.Delay(50);
        }

        throw new TimeoutException("Message did not arrive.");
    }

    [Fact]
    public async Task Outbox_retry_and_duplicate_delivery_finalize_order_without_second_stock_change()
    {
        var(owner, id) = await Seed();
        var topology = Topology();
        await using var session = await RabbitSession.Open(fixture.Settings, topology, default);
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var outbox = scope.ServiceProvider.GetRequiredService<InventoryOutbox>();
            await Assert.ThrowsAsync<IOException>(() => outbox.Process(async (eventId, body, ct) =>
            {
                await session.Publish(eventId.ToString(), "inventory.reservation-outcome.v1", body, ct);
                throw new IOException("Lost publish confirmation");
            }, default));
        }

        var first = await Read(session);
        await Handle(first);
        await session.Ack(first.Tag, default);
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var s = scope.ServiceProvider;
            Assert.Equal(1, await s.GetRequiredService<InventoryOutbox>().Process((eventId, body, ct) => session.Publish(eventId.ToString(), "inventory.reservation-outcome.v1", body, ct), default));
            Assert.Equal("Confirmed", Assert.Single((await s.GetRequiredService<IDemoOrders>().Read(owner, 1, default)).Items).Status);
            Assert.Equal(0, fixture.Remote.ConsumeCalls);
        }

        var duplicate = await Read(session);
        await Task.WhenAll(Handle(duplicate), Handle(duplicate));
        await session.Ack(duplicate.Tag, default);
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var s = scope.ServiceProvider;
            Assert.Equal(1, await s.GetRequiredService<OrderingDbContext>().ReservationInbox.CountAsync(x => x.Id == Guid.Parse(first.MessageId!)));
            await s.GetRequiredService<IOrderCancellations>().Cancel(id, "Cerere client", "admin", default);
            await s.GetRequiredService<IStockReservations>().Restore(id, id, default);
            await s.GetRequiredService<InventoryOutbox>().Process((eventId, body, ct) => session.Publish(eventId.ToString(), "inventory.reservation-outcome.v1", body, ct), default);
        }

        var restored = await Read(session);
        await Handle(restored);
        await session.Ack(restored.Tag, default);
        // Different event ID and old outcome still cannot regress the restored order.
        var old = JsonSerializer.Deserialize<Outcome>(first.Body)!with
        {
            EventId = Guid.NewGuid()
        };
        await Handle(first with { MessageId = old.EventId.ToString(), Body = JsonSerializer.SerializeToUtf8Bytes(old) });
        await using var check = fixture.Services.CreateAsyncScope();
        Assert.Equal("Cancelled", Assert.Single((await check.ServiceProvider.GetRequiredService<IDemoOrders>().Read(owner, 1, default)).Items).Status);
        Assert.Equal("Restored", (await check.ServiceProvider.GetRequiredService<IReservationPlans>().Read(id, default))!.Status);
        var inventory = check.ServiceProvider.GetRequiredService<InventoryDbContext>();
        Assert.Single(await inventory.Consumptions.Where(x => x.ReservationId == id).ToListAsync());
        Assert.Single(await inventory.Restorations.Where(x => x.ReservationId == id).ToListAsync());
    }

    [Fact]
    public async Task Unacknowledged_delivery_survives_disconnect_and_reconnect()
    {
        var topology = Topology();
        var id = Guid.NewGuid().ToString();
        await using (var session = await RabbitSession.Open(fixture.Settings, topology, default))
        {
            await session.Publish(id, "test", Encoding.UTF8.GetBytes("{}"), default);
            Assert.Equal(id, (await Read(session)).MessageId);
        }

        await using var next = await RabbitSession.Open(fixture.Settings, topology, default);
        var retry = await Read(next);
        Assert.Equal(id, retry.MessageId);
        await next.Ack(retry.Tag, default);
        Assert.Null(await next.Read(default));
    }

    [Fact]
    public async Task Retry_is_persistent_and_exhausted_or_invalid_messages_reach_dead_letter_queue()
    {
        var topology = Topology();
        await using var session = await RabbitSession.Open(fixture.Settings, topology, default);
        var id = Guid.NewGuid().ToString();
        await session.Publish(id, "test", Encoding.UTF8.GetBytes("{}"), default, 3);
        var original = await Read(session);
        await DeliveryProcessor.Process(session, original, _ => throw new IOException(), _ => false, default);
        var retry = await Read(session);
        Assert.Equal(4, retry.Attempt);
        await DeliveryProcessor.Process(session, retry, _ => throw new IOException(), _ => false, default);
        var dead = await Read(session, true);
        Assert.Equal(5, dead.Attempt);
        Assert.Equal(id, dead.MessageId);
        await session.Ack(dead.Tag, default);
        await session.Publish("invalid", "unknown", Encoding.UTF8.GetBytes("bad"), default);
        var invalid = await Read(session);
        await DeliveryProcessor.Process(session, invalid, _ => Handle(invalid), e => e is InvalidReservationEventException, default);
        var poisoned = await Read(session, true);
        Assert.Equal("invalid", poisoned.MessageId);
        await session.Ack(poisoned.Tag, default);
        Assert.Null(await session.Read(default));
    }

    [Fact]
    public async Task Invalid_contract_has_no_inbox_or_business_effect()
    {
        var id = Guid.NewGuid();
        var body = Encoding.UTF8.GetBytes("{}");
        await Assert.ThrowsAsync<InvalidReservationEventException>(() => Handle(new(1, id.ToString(), "inventory.reservation-outcome.v1", body, 0)));
        await using var scope = fixture.Services.CreateAsyncScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<OrderingDbContext>().ReservationInbox.AnyAsync(x => x.Id == id));
    }

    [Fact]
    public async Task Same_event_id_with_different_payload_is_rejected()
    {
        var id = Guid.NewGuid();
        var unknown = new Outcome(id, Guid.NewGuid(), Guid.NewGuid(), "Consumed", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, [new OutcomeLine(Guid.NewGuid(), 1)]);
        var d = new BrokerDelivery(1, id.ToString(), "inventory.reservation-outcome.v1", JsonSerializer.SerializeToUtf8Bytes(unknown), 0);
        await Handle(d);
        await Assert.ThrowsAsync<InvalidReservationEventException>(() => Handle(d with { Body = JsonSerializer.SerializeToUtf8Bytes(unknown with { Status = "Restored" }) }));
    }

    [Fact]
    public async Task Mandatory_publication_rejects_a_message_without_any_route()
    {
        var topology = Topology();
        await using var session = await RabbitSession.Open(fixture.Settings, topology, default);
        var settings = fixture.Settings;
        var factory = new RabbitMQ.Client.ConnectionFactory
        {
            HostName = settings.Host,
            Port = settings.Port,
            VirtualHost = settings.VirtualHost,
            UserName = settings.Username,
            Password = settings.Password
        };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.QueueUnbindAsync(topology.Queue, topology.Exchange, topology.RoutingKey, null);
        var failure = await Assert.ThrowsAsync<RabbitMQ.Client.Exceptions.PublishReturnException>(() => session.Publish(Guid.NewGuid().ToString(), "test", Encoding.UTF8.GetBytes("{}"), default));
        Assert.True(failure.IsReturn);
    }
}
