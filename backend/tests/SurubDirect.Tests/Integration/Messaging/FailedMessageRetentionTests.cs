using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.Messaging;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Infrastructure;
using Xunit;

namespace SurubDirect.Tests;
public sealed class FailedMessageRetentionTests(MessagingFixture fixture) : IClassFixture<MessagingFixture>
{
    private async Task<(Guid Id, BrokerDelivery Delivery)> Seed(bool inbox = true, bool matchingHash = true, bool recentInbox = false, bool published = true)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        await db.FailedReservationMessages.ExecuteDeleteAsync();
        await db.ReservationInbox.ExecuteDeleteAsync();
        var eventId = Guid.NewGuid();
        var body = JsonSerializer.SerializeToUtf8Bytes(new ReservationOutcome(eventId, Guid.NewGuid(), Guid.NewGuid(), "Consumed", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, [new(Guid.NewGuid(), 1)]));
        // Noncanonical header: identity must come from parsing, not textual GUID equality.
        var delivery = new BrokerDelivery(1, eventId.ToString("B").ToUpperInvariant(), "inventory.reservation-outcome.v1", body, 5);
        await scope.ServiceProvider.GetRequiredService<FailedMessageStore>().Capture(delivery, default);
        var row = await db.FailedReservationMessages.SingleAsync();
        var old = DateTimeOffset.UtcNow.AddDays(-60);
        row.CapturedAt = old;
        row.RequestedAt = old;
        row.ReplayRequestId = Guid.NewGuid();
        row.ReplayReason = "Dependență reparată";
        row.RequestedBy = "administrator";
        row.PublishedAt = published ? old : null;
        if (inbox)
            db.ReservationInbox.Add(new() { Id = eventId, PayloadHash = matchingHash ? Convert.ToHexString(SHA256.HashData(body)) : new string ('F', 64), ReceivedAt = recentInbox ? DateTimeOffset.UtcNow : old });
        await db.SaveChangesAsync();
        return (row.Id, delivery);
    }

    private async Task<SurubDirect.Ordering.Contracts.FailedMessageRetentionResult> Run(bool dryRun)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IFailedMessageRetention>().Run(30, 1, dryRun, default);
    }

    [Fact]
    public async Task Simulation_preserves_payload_and_real_retention_preserves_deduplication_and_audit()
    {
        var(id, delivery) = await Seed();
        Assert.Equal(1, (await Run(true)).Candidates);
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var before = await db.FailedReservationMessages.AsNoTracking().SingleAsync();
        Assert.Equal(delivery.Body, before.Payload);
        Assert.Null(before.PayloadRemovedAt);
        Assert.Equal(1, (await Run(false)).Removed);
        await scope.ServiceProvider.GetRequiredService<FailedMessageStore>().Capture(delivery, default);
        db.ChangeTracker.Clear();
        var after = await db.FailedReservationMessages.SingleAsync();
        Assert.Equal(id, after.Id);
        Assert.Empty(after.Payload);
        Assert.NotNull(after.PayloadRemovedAt);
        Assert.Equal(before.OriginalSize, after.OriginalSize);
        Assert.Equal(before.Fingerprint, after.Fingerprint);
        Assert.Equal(before.PayloadHash, after.PayloadHash);
        Assert.Equal(before.ReplayRequestId, after.ReplayRequestId);
        Assert.Equal(before.ReplayReason, after.ReplayReason);
        Assert.Equal(before.RequestedBy, after.RequestedBy);
        Assert.Equal(before.PublishedAt, after.PublishedAt);
        var result = await scope.ServiceProvider.GetRequiredService<IMessagingAdministration>().Replay(id, new(before.ReplayRequestId!.Value, before.ReplayReason!), "another-admin", default);
        Assert.Equal(before.RequestedBy, result.RequestedBy);
        Assert.NotNull(result.PayloadRemovedAt);
        Assert.Equal(0, await scope.ServiceProvider.GetRequiredService<FailedMessageStore>().PublishPending((message, ct) => throw new Exception("Must not republish removed body"), default));
        await scope.ServiceProvider.GetRequiredService<ReservationInbox>().Handle(delivery.MessageId, delivery.Type, delivery.Body, default);
        Assert.Equal(1, await db.ReservationInbox.CountAsync());
        Assert.Equal(0, (await Run(false)).Removed);
    }

    [Theory]
    [InlineData(false, true, false, true)]
    [InlineData(true, false, false, true)]
    [InlineData(true, true, true, true)]
    [InlineData(true, true, false, false)]
    public async Task Missing_or_different_or_recent_inbox_and_unpublished_replay_are_retained(bool inbox, bool match, bool recent, bool published)
    {
        var(_, delivery) = await Seed(inbox, match, recent, published);
        Assert.Equal(0, (await Run(false)).Removed);
        await using var scope = fixture.Services.CreateAsyncScope();
        Assert.Equal(delivery.Body, (await scope.ServiceProvider.GetRequiredService<OrderingDbContext>().FailedReservationMessages.SingleAsync()).Payload);
    }

    [Fact]
    public async Task Unresolved_invalid_and_changed_bodies_remain_available()
    {
        await Seed();
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var row = await db.FailedReservationMessages.SingleAsync();
        row.Payload = [1, 2, 3];
        await db.SaveChangesAsync();
        Assert.Equal(0, (await Run(false)).Removed);
        await scope.ServiceProvider.GetRequiredService<FailedMessageStore>().Capture(new(2, "invalid", "invalid", [1, 2], 5), default);
        Assert.Equal(0, (await Run(false)).Removed);
        Assert.Equal(2, await db.FailedReservationMessages.CountAsync());
    }

    [Fact]
    public async Task Concurrent_retention_only_cleans_once_and_skips_locked_rows()
    {
        await Seed();
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            await db.FailedReservationMessages.FromSqlRaw("""SELECT * FROM ordering."FailedReservationMessages" FOR UPDATE""").ToListAsync();
            Assert.Equal(0, (await Run(false)).Removed);
            await tx.RollbackAsync();
        }

        var results = await Task.WhenAll(Run(false), Run(false));
        Assert.Equal(1, results.Sum(x => x.Removed));
    }

    [Theory]
    [InlineData(29, 1)]
    [InlineData(3651, 1)]
    [InlineData(30, 0)]
    [InlineData(30, 1001)]
    public async Task Invalid_parameters_are_rejected(int days, int batch)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => scope.ServiceProvider.GetRequiredService<IFailedMessageRetention>().Run(days, batch, false, default));
    }
}
