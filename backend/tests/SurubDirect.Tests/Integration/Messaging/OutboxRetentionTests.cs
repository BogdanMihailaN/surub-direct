using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.Inventory.Application;
using SurubDirect.Inventory.Infrastructure;
using Xunit;

namespace SurubDirect.Tests;
public sealed class OutboxRetentionTests(MessagingFixture fixture) : IClassFixture<MessagingFixture>
{
    private async Task<InventoryEvent[]> Seed()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        await db.ReservationOutbox.ExecuteDeleteAsync();
        var now = DateTimeOffset.UtcNow;
        InventoryEvent Row(DateTimeOffset? published) => new()
        {
            Id = Guid.NewGuid(),
            ReservationId = Guid.NewGuid(),
            Status = "Consumed",
            Payload = """{"example":"payload retained until eligible"}""",
            OccurredAt = now.AddDays(-90),
            PublishedAt = published
        };
        var rows = new[]
        {
            Row(now.AddDays(-60)),
            Row(now.AddDays(-59)),
            Row(now.AddDays(-7)),
            Row(null)
        };
        db.ReservationOutbox.AddRange(rows);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return await db.ReservationOutbox.OrderBy(x => x.PublishedAt).ToArrayAsync();
    }

    private async Task<SurubDirect.Inventory.Contracts.OutboxRetentionResult> Run(bool dryRun, int batchSize = 100)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IOutboxRetention>().Run(30, batchSize, dryRun, default);
    }

    [Fact]
    public async Task Simulation_counts_only_eligible_published_payloads_without_modifying_rows()
    {
        var before = await Seed();
        var result = await Run(true, 1);
        Assert.Equal(1, result.Candidates);
        Assert.Equal(0, result.Removed);
        Assert.True(result.DryRun);
        await using var scope = fixture.Services.CreateAsyncScope();
        var rows = await scope.ServiceProvider.GetRequiredService<InventoryDbContext>().ReservationOutbox.ToArrayAsync();
        Assert.All(rows, x =>
        {
            Assert.Null(x.PayloadRemovedAt);
            Assert.Null(x.PayloadHash);
            Assert.Equal(before.Single(b => b.Id == x.Id).Payload, x.Payload);
        });
    }

    [Fact]
    public async Task Removal_keeps_audit_unique_keys_and_never_republishes_scrubbed_payloads()
    {
        var before = await Seed();
        Assert.Equal(2, (await Run(false)).Removed);
        Assert.Equal(0, (await Run(false)).Removed);
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var rows = await db.ReservationOutbox.AsNoTracking().ToArrayAsync();
        Assert.Equal(before.Length, rows.Length);
        foreach (var row in rows)
        {
            var old = before.Single(x => x.Id == row.Id);
            Assert.Equal(old.PublishedAt, row.PublishedAt);
            Assert.Equal(old.OccurredAt, row.OccurredAt);
            Assert.Equal(old.ReservationId, row.ReservationId);
            Assert.Equal(old.Status, row.Status);
            if (row.PayloadRemovedAt is not null)
            {
                Assert.Equal("{}", row.Payload);
                Assert.Equal(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(old.Payload))), row.PayloadHash);
            }
            else
                Assert.Equal(old.Payload, row.Payload);
        }

        var published = new List<Guid>();
        var publisher = scope.ServiceProvider.GetRequiredService<InventoryOutbox>();
        await publisher.Process((id, body, ct) =>
        {
            published.Add(id);
            Assert.DoesNotContain(id, rows.Where(x => x.PayloadRemovedAt != null).Select(x => x.Id));
            return Task.CompletedTask;
        }, default);
        Assert.Equal(before.Single(x => x.PublishedAt == null).Id, Assert.Single(published));
        var duplicate = new InventoryEvent
        {
            Id = Guid.NewGuid(),
            ReservationId = rows[0].ReservationId,
            Status = rows[0].Status,
            Payload = "{}",
            OccurredAt = DateTimeOffset.UtcNow
        };
        db.ReservationOutbox.Add(duplicate);
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Concurrent_batches_do_not_remove_the_same_payload_twice()
    {
        await Seed();
        var results = await Task.WhenAll(Run(false, 1), Run(false, 1));
        Assert.Equal(2, results.Sum(x => x.Removed));
        Assert.Equal(0, (await Run(false)).Candidates);
    }

    [Fact]
    public async Task Locked_row_is_skipped_and_becomes_eligible_after_unlock()
    {
        await Seed();
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.ReservationOutbox.FromSqlRaw("""SELECT * FROM inventory."ReservationOutbox" WHERE "PublishedAt" < now() - interval '30 days' FOR UPDATE""").ToListAsync();
        Assert.Equal(0, (await Run(false)).Removed);
        await tx.RollbackAsync();
        Assert.Equal(2, (await Run(false)).Removed);
    }

    [Fact]
    public async Task Lost_publisher_confirmation_does_not_make_pending_message_eligible()
    {
        await Seed();
        await Run(false);
        await using var scope = fixture.Services.CreateAsyncScope();
        var publisher = scope.ServiceProvider.GetRequiredService<InventoryOutbox>();
        await Assert.ThrowsAsync<IOException>(() => publisher.Process((id, body, ct) => throw new IOException("Lost confirmation"), default));
        Assert.Equal(0, (await Run(false)).Removed);
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        db.ChangeTracker.Clear();
        var pending = await db.ReservationOutbox.SingleAsync(x => x.PublishedAt == null);
        Assert.Null(pending.PayloadRemovedAt);
        Assert.NotEqual("{}", pending.Payload);
    }

    [Fact]
    public async Task Database_rejects_payload_removal_for_an_unpublished_message()
    {
        await Seed();
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var pending = await db.ReservationOutbox.SingleAsync(x => x.PublishedAt == null);
        pending.PayloadRemovedAt = DateTimeOffset.UtcNow;
        pending.PayloadHash = new string ('A', 64);
        pending.Payload = "{}";
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Theory]
    [InlineData(29, 100)]
    [InlineData(3651, 100)]
    [InlineData(30, 0)]
    [InlineData(30, 1001)]
    public async Task Invalid_retention_parameters_are_rejected(int days, int batch)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => scope.ServiceProvider.GetRequiredService<IOutboxRetention>().Run(days, batch, false, default));
    }
}
