using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Inventory.Application;
using SurubDirect.Inventory.Domain;
using SurubDirect.Inventory.Infrastructure;
using Xunit;

namespace SurubDirect.Tests;
public sealed class StockReservationTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    private async Task<Guid> Stock(int quantity, Guid? desiredId = null)
    {
        var id = desiredId ?? Guid.NewGuid();
        await using var s = fixture.Services.CreateAsyncScope();
        await s.ServiceProvider.GetRequiredService<IInventoryRepository>().Receive(StockReceipt.Create(Guid.NewGuid(), id, quantity, "TEST", "test"), default);
        return id;
    }

    private async Task<StockReservation> Reserve(Guid id, Guid order, params ReservationLine[] lines)
    {
        await using var s = fixture.Services.CreateAsyncScope();
        return await s.ServiceProvider.GetRequiredService<IStockReservations>().Reserve(StockReservation.Create(id, order, 120, lines, DateTimeOffset.UtcNow), default);
    }

    private async Task<StockBalance> Balance(Guid id)
    {
        await using var s = fixture.Services.CreateAsyncScope();
        return await s.ServiceProvider.GetRequiredService<InventoryDbContext>().Balances.AsNoTracking().SingleAsync(x => x.VariantId == id);
    }

    [Fact]
    public async Task Multi_sku_failure_rolls_back_every_balance_and_retry_can_succeed()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() }.Order().ToArray();
        var a = await Stock(100, ids[0]);
        var b = await Stock(1, ids[1]);
        var id = Guid.NewGuid();
        var order = Guid.NewGuid();
        await Assert.ThrowsAsync<BusinessException>(() => Reserve(id, order, new(a, 10), new(b, 2)));
        Assert.Equal(0, (await Balance(a)).Reserved);
        Assert.Equal(0, (await Balance(b)).Reserved);
        await using var s = fixture.Services.CreateAsyncScope();
        Assert.False(await s.ServiceProvider.GetRequiredService<InventoryDbContext>().Reservations.AnyAsync(x => x.Id == id));
        await s.ServiceProvider.GetRequiredService<IInventoryRepository>().Receive(StockReceipt.Create(Guid.NewGuid(), b, 1, "TOPUP", "test"), default);
        await Reserve(id, order, new(a, 10), new(b, 2));
        Assert.Equal(10, (await Balance(a)).Reserved);
        Assert.Equal(2, (await Balance(b)).Reserved);
        Assert.Equal(100, (await Balance(a)).OnHand);
    }

    [Fact]
    public async Task Concurrent_requests_do_not_oversell_and_adjustments_respect_reservations()
    {
        var sku = await Stock(100);
        async Task<bool> Attempt()
        {
            try
            {
                await Reserve(Guid.NewGuid(), Guid.NewGuid(), new ReservationLine(sku, 70));
                return true;
            }
            catch (BusinessException e)
            {
                Assert.Equal(409, e.Status);
                return false;
            }
        }

        Assert.Single(await Task.WhenAll(Attempt(), Attempt()), x => x);
        Assert.Equal(70, (await Balance(sku)).Reserved);
        await using var s = fixture.Services.CreateAsyncScope();
        await Assert.ThrowsAsync<BusinessException>(() => s.ServiceProvider.GetRequiredService<IStockLedger>().Adjust(StockAdjustment.Create(Guid.NewGuid(), sku, -31, 100, 70, "ADJ", "TEST", "test"), default));
        Assert.Equal(100, (await Balance(sku)).OnHand);
    }

    [Fact]
    public async Task Concurrent_duplicates_are_idempotent_and_changed_payload_is_rejected()
    {
        var a = await Stock(100);
        var b = await Stock(100);
        var id = Guid.NewGuid();
        var order = Guid.NewGuid();
        await Task.WhenAll(Reserve(id, order, new(a, 20), new(b, 30)), Reserve(id, order, new(b, 30), new(a, 20)));
        Assert.Equal(20, (await Balance(a)).Reserved);
        Assert.Equal(30, (await Balance(b)).Reserved);
        await Assert.ThrowsAsync<BusinessException>(() => Reserve(id, order, new(a, 21), new(b, 30)));
        await using var s = fixture.Services.CreateAsyncScope();
        var repo = s.ServiceProvider.GetRequiredService<IStockReservations>();
        await repo.Release(id, order, default);
        await repo.Release(id, order, default);
        Assert.Equal(ReservationStatus.Released, (await Reserve(id, order, new(a, 20), new(b, 30))).Status);
        Assert.Equal(0, (await Balance(a)).Reserved);
        Assert.Equal(0, (await Balance(b)).Reserved);
    }

    [Fact]
    public async Task Early_cancellation_fences_delayed_reserve_and_wrong_order_cannot_release()
    {
        var sku = await Stock(100);
        var id = Guid.NewGuid();
        var order = Guid.NewGuid();
        await using var s = fixture.Services.CreateAsyncScope();
        var repo = s.ServiceProvider.GetRequiredService<IStockReservations>();
        var cancelled = await repo.Release(id, order, default);
        Assert.Equal(ReservationStatus.Released, cancelled.Status);
        Assert.Equal(ReservationStatus.Released, (await Reserve(id, order, new ReservationLine(sku, 20))).Status);
        Assert.Equal(0, (await Balance(sku)).Reserved);
        var active = await Reserve(Guid.NewGuid(), order, new ReservationLine(sku, 20));
        await Assert.ThrowsAsync<BusinessException>(() => repo.Release(active.Id, Guid.NewGuid(), default));
        Assert.Equal(20, (await Balance(sku)).Reserved);
    }

    [Fact]
    public async Task Expiry_and_release_race_returns_reserved_stock_once_and_never_reactivates()
    {
        var sku = await Stock(100);
        var id = Guid.NewGuid();
        var order = Guid.NewGuid();
        await Reserve(id, order, new ReservationLine(sku, 20));
        await using (var s = fixture.Services.CreateAsyncScope())
            await s.ServiceProvider.GetRequiredService<InventoryDbContext>().Reservations.Where(x => x.Id == id).ExecuteUpdateAsync(x => x.SetProperty(r => r.ExpiresAt, DateTimeOffset.UtcNow.AddSeconds(-1)));
        async Task Expire()
        {
            await using var s = fixture.Services.CreateAsyncScope();
            await s.ServiceProvider.GetRequiredService<IStockReservations>().Expire(100, default);
        }

        async Task Release()
        {
            await using var s = fixture.Services.CreateAsyncScope();
            await s.ServiceProvider.GetRequiredService<IStockReservations>().Release(id, order, default);
        }

        await Task.WhenAll(Expire(), Release(), Expire());
        var balance = await Balance(sku);
        Assert.Equal(0, balance.Reserved);
        Assert.Equal(100, balance.OnHand);
        Assert.Equal(ReservationStatus.Expired, (await Reserve(id, order, new ReservationLine(sku, 20))).Status);
        Assert.Equal(0, (await Balance(sku)).Reserved);
    }
}
