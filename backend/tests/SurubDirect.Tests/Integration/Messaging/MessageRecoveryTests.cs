using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Messaging;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Contracts;
using SurubDirect.Ordering.Infrastructure;
using Xunit;

namespace SurubDirect.Tests;
public sealed class MessageRecoveryTests(MessagingFixture fixture) : IClassFixture<MessagingFixture>
{
    [Fact]
    public async Task Failure_monitor_reports_global_aggregates()
    {
        await Capture(Delivery());
        await using var scope = fixture.Services.CreateAsyncScope();
        var summary = await scope.ServiceProvider.GetRequiredService<IMessageFailureMonitor>().Read(default);
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        Assert.Equal(await db.FailedReservationMessages.CountAsync(x => x.RequestedAt == null), summary.AwaitingReview);
        Assert.Equal(await db.FailedReservationMessages.CountAsync(x => x.RequestedAt != null && x.PublishedAt == null), summary.PendingReplays);
        Assert.True(summary.AwaitingReview > 0);
    }

    private static BrokerDelivery Delivery(Guid? replay = null)
    {
        var id = Guid.NewGuid();
        var message = new ReservationOutcome(id, Guid.NewGuid(), Guid.NewGuid(), "Consumed", DateTimeOffset.UtcNow.AddMinutes(15), DateTimeOffset.UtcNow, [new(Guid.NewGuid(), 1)]);
        return new(1, id.ToString(), "inventory.reservation-outcome.v1", JsonSerializer.SerializeToUtf8Bytes(message), 5, replay);
    }

    private async Task Capture(BrokerDelivery delivery)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<FailedMessageStore>().Capture(delivery, default);
    }

    [Fact]
    public async Task Capture_deduplicates_redelivery_but_keeps_a_new_replay_failure()
    {
        var delivery = Delivery();
        await Task.WhenAll(Capture(delivery), Capture(delivery));
        await Capture(delivery with { ReplayId = Guid.NewGuid() });
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var records = await db.FailedReservationMessages.Where(x => x.MessageId == delivery.MessageId).ToListAsync();
        Assert.Equal(2, records.Count);
        Assert.All(records, x => Assert.Equal(delivery.Body, x.Payload));
        Assert.Single(records, x => x.PreviousReplayId is null);
    }

    [Fact]
    public async Task Replay_is_audited_idempotent_and_survives_lost_publisher_response()
    {
        var delivery = Delivery();
        await Capture(delivery);
        Guid id;
        var request = new RequestMessageReplay(Guid.NewGuid(), "Dependența a fost reparată");
        await using (var scope = fixture.Services.CreateAsyncScope())
            id = (await scope.ServiceProvider.GetRequiredService<OrderingDbContext>().FailedReservationMessages.SingleAsync(x => x.MessageId == delivery.MessageId)).Id;
        async Task<FailedMessageDto> Request(string actor)
        {
            await using var scope = fixture.Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<ReplayMessageHandler>().Handle(id, request, actor, default);
        }

        var results = await Task.WhenAll(Request("admin"), Request("admin"));
        Assert.All(results, x => Assert.Equal(request.RequestId, x.ReplayRequestId));
        Assert.Equal("admin", (await Request("other-admin")).RequestedBy);
        await using var work = fixture.Services.CreateAsyncScope();
        var store = work.ServiceProvider.GetRequiredService<FailedMessageStore>();
        BrokerDelivery? published = null;
        await Assert.ThrowsAsync<IOException>(() => store.PublishPending((value, _) =>
        {
            published = value;
            throw new IOException("Confirmation lost");
        }, default));
        Assert.NotNull(published);
        Assert.Equal(delivery.Body, published!.Body);
        Assert.Equal(request.RequestId, published.ReplayId);
        Assert.Equal(0, published.Attempt);
        var first = published;
        await store.PublishPending((value, _) =>
        {
            published = value;
            return Task.CompletedTask;
        }, default);
        Assert.Equal(first.ReplayId, published.ReplayId);
        Assert.Equal(first.Body, published.Body);
        var row = await work.ServiceProvider.GetRequiredService<OrderingDbContext>().FailedReservationMessages.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.NotNull(row.PublishedAt);
        Assert.Equal(request.Reason, row.ReplayReason);
        Assert.Equal(0, await store.PublishPending((_, _) => throw new Exception("must not publish twice"), default));
        var handler = work.ServiceProvider.GetRequiredService<ReplayMessageHandler>();
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => handler.Handle(id, request with { Reason = "alt motiv" }, "admin", default))).Status);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => handler.Handle(id, new(Guid.NewGuid(), "Altă cerere"), "admin", default))).Status);
    }

    [Fact]
    public async Task Invalid_contract_and_oversized_message_are_retained_without_replay()
    {
        var malformed = Delivery()with
        {
            Body = "{}"u8.ToArray()
        };
        await Capture(malformed);
        var oversized = Delivery()with
        {
            Body = new byte[65537]
        };
        await Capture(oversized);
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        foreach (var message in new[]
        {
            malformed,
            oversized
        }

        )
        {
            var row = await db.FailedReservationMessages.SingleAsync(x => x.MessageId == message.MessageId);
            Assert.False(row.CanReplay);
            Assert.Equal(message.Body.Length, row.OriginalSize);
            Assert.Equal(message.Body, row.Payload);
            Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => scope.ServiceProvider.GetRequiredService<ReplayMessageHandler>().Handle(row.Id, new(Guid.NewGuid(), "Reîncercare"), "admin", default))).Status);
        }
    }

    [Fact]
    public async Task Request_id_cannot_be_reused_for_another_failed_message()
    {
        var a = Delivery();
        var b = Delivery();
        await Capture(a);
        await Capture(b);
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var handler = scope.ServiceProvider.GetRequiredService<ReplayMessageHandler>();
        var request = new RequestMessageReplay(Guid.NewGuid(), "Reparat");
        var rows = await db.FailedReservationMessages.Where(x => x.MessageId == a.MessageId || x.MessageId == b.MessageId).ToListAsync();
        await handler.Handle(rows[0].Id, request, "admin", default);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => handler.Handle(rows[1].Id, request, "admin", default))).Status);
        // Complete the pending publication so other tests have no unrelated work.
        await scope.ServiceProvider.GetRequiredService<FailedMessageStore>().PublishPending((_, _) => Task.CompletedTask, default);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    public async Task Replay_requires_a_reason(string reason)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        Assert.Equal(400, (await Assert.ThrowsAsync<BusinessException>(() => scope.ServiceProvider.GetRequiredService<ReplayMessageHandler>().Handle(Guid.NewGuid(), new(Guid.NewGuid(), reason), "admin", default))).Status);
    }

    [Fact]
    public async Task Pagination_and_missing_identifiers_are_validated()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        Assert.Equal(400, (await Assert.ThrowsAsync<BusinessException>(() => services.GetRequiredService<GetMessagingStatusHandler>().Handle(0, default))).Status);
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => services.GetRequiredService<ReplayMessageHandler>().Handle(Guid.NewGuid(), new(Guid.NewGuid(), "Reparat"), "admin", default))).Status);
    }

    [Fact]
    public async Task Broker_monitor_is_read_only_and_distinguishes_unavailable_from_empty()
    {
        var id = Guid.NewGuid().ToString("N");
        var topology = new BrokerTopology("monitor." + id, "queue." + id, "outcome", "dead." + id);
        var monitor = new RabbitBrokerMonitor(fixture.Settings, topology);
        var absent = await monitor.Read(default);
        Assert.False(absent.Available);
        Assert.Null(absent.Ready);
        await using var session = await RabbitSession.Open(fixture.Settings, topology, default);
        var empty = await monitor.Read(default);
        Assert.True(empty.Available);
        Assert.Equal(0u, empty.Ready);
        var delivery = Delivery(Guid.NewGuid());
        await session.Publish(delivery.MessageId!, delivery.Type!, delivery.Body, default, 4, replayId: delivery.ReplayId);
        BrokerDelivery? value = null;
        for (var attempt = 0; attempt < 100 && value is null; attempt++)
        {
            value = await session.Read(default);
            if (value is null)
                await Task.Delay(20);
        }

        Assert.NotNull(value);
        Assert.Equal(delivery.ReplayId, value!.ReplayId);
        await DeliveryProcessor.Process(session, value, _ => throw new IOException(), _ => false, default);
        BrokerDelivery? dead = null;
        for (var attempt = 0; attempt < 100 && dead is null; attempt++)
        {
            dead = await session.Read(default, true);
            if (dead is null)
                await Task.Delay(20);
        }

        Assert.NotNull(dead);
        Assert.Equal(delivery.ReplayId, dead!.ReplayId);
        await session.Ack(dead.Tag, default);
        var disabled = await new DisabledBrokerMonitor().Read(default);
        Assert.False(disabled.Configured);
        Assert.Null(disabled.Ready);
    }
}
