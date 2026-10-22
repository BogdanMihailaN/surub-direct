using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Inventory.Application;
using SurubDirect.Inventory.Domain;
using SurubDirect.Inventory.Infrastructure;
using Xunit;

namespace SurubDirect.Tests;
public sealed class StockRestorationTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    private async Task<StockReservation> Seed()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var lines = new[]
        {
            new ReservationLine(Guid.NewGuid(), 20),
            new ReservationLine(Guid.NewGuid(), 30)
        }.OrderBy(x => x.VariantId).ToArray();
        foreach (var line in lines)
            await services.GetRequiredService<IInventoryRepository>().Receive(StockReceipt.Create(Guid.NewGuid(), line.VariantId, 100, "INITIAL", "test"), default);
        return await services.GetRequiredService<IStockReservations>().Reserve(StockReservation.Create(Guid.NewGuid(), Guid.NewGuid(), 120, lines, DateTimeOffset.UtcNow), default);
    }

    private async Task<StockReservation> Consume(StockReservation r)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IStockReservations>().Consume(r.Id, r.OrderId, default);
    }

    private async Task<StockReservation> Restore(StockReservation r)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IStockReservations>().Restore(r.Id, r.OrderId, default);
    }

    [Fact]
    public async Task Concurrent_replays_restore_once_preserve_consumption_and_fence_delayed_consume()
    {
        var r = await Seed();
        var consumed = await Consume(r);
        var results = await Task.WhenAll(Restore(r), Restore(r), Restore(r));
        Assert.All(results, x =>
        {
            Assert.Equal(ReservationStatus.Restored, x.Status);
            Assert.Equal(consumed.EndedAt, x.EndedAt);
        });
        Assert.Equal(ReservationStatus.Restored, (await Consume(r)).Status);
        await using var scope = fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IStockReservations>();
        Assert.Equal(ReservationStatus.Restored, (await repo.Release(r.Id, r.OrderId, default)).Status);
        Assert.Equal(ReservationStatus.Restored, (await repo.Reserve(r, default)).Status);
        foreach (var line in r.Lines)
        {
            var ledger = await scope.ServiceProvider.GetRequiredService<IStockLedger>().Read(line.VariantId, 1, default);
            Assert.Equal(100, ledger.OnHand);
            Assert.Equal(0, ledger.Reserved);
            Assert.Equal(-line.Quantity, Assert.Single(ledger.Items, x => x.Kind == "Consumption").Quantity);
            Assert.Equal(line.Quantity, Assert.Single(ledger.Items, x => x.Kind == "Restoration").Quantity);
        }
    }

    [Fact]
    public async Task Active_released_unknown_and_wrong_order_cannot_restore()
    {
        var r = await Seed();
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => Restore(r))).Status);
        await using var scope = fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IStockReservations>();
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => repo.Restore(Guid.NewGuid(), r.OrderId, default))).Status);
        await Consume(r);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => repo.Restore(r.Id, Guid.NewGuid(), default))).Status);
        var released = await Seed();
        await repo.Release(released.Id, released.OrderId, default);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => Restore(released))).Status);
    }

    [Fact]
    public async Task Later_overflow_rolls_back_every_balance_and_ledger_then_retry_succeeds()
    {
        var r = await Seed();
        await Consume(r);
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var last = r.Lines.Last();
        await db.Balances.Where(x => x.VariantId == last.VariantId).ExecuteUpdateAsync(x => x.SetProperty(v => v.OnHand, int.MaxValue));
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => Restore(r))).Status);
        Assert.False(await db.Restorations.AnyAsync(x => x.ReservationId == r.Id));
        Assert.Equal(ReservationStatus.Consumed, (await db.Reservations.AsNoTracking().SingleAsync(x => x.Id == r.Id)).Status);
        Assert.Equal(100 - r.Lines.First().Quantity, (await db.Balances.AsNoTracking().SingleAsync(x => x.VariantId == r.Lines.First().VariantId)).OnHand);
        await db.Balances.Where(x => x.VariantId == last.VariantId).ExecuteUpdateAsync(x => x.SetProperty(v => v.OnHand, 100 - last.Quantity));
        await Restore(r);
        Assert.Equal(2, await db.Restorations.CountAsync(x => x.ReservationId == r.Id));
    }
}
