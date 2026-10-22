using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Inventory.Application;
using SurubDirect.Inventory.Domain;
using SurubDirect.Inventory.Infrastructure;
using Xunit;

namespace SurubDirect.Tests;
public sealed class StockConsumptionTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
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

    [Fact]
    public async Task Concurrent_retries_consume_all_skus_once_and_keep_an_immutable_ledger()
    {
        var r = await Seed();
        var results = await Task.WhenAll(Consume(r), Consume(r), Consume(r));
        Assert.All(results, result => Assert.Equal(ReservationStatus.Consumed, result.Status));
        Assert.Single(results.Select(x => x.EndedAt).Distinct());
        await using var scope = fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IStockReservations>();
        Assert.Equal(ReservationStatus.Consumed, (await repo.Release(r.Id, r.OrderId, default)).Status);
        Assert.Equal(ReservationStatus.Consumed, (await repo.Reserve(r, default)).Status);
        foreach (var line in r.Lines)
        {
            var ledger = await scope.ServiceProvider.GetRequiredService<IStockLedger>().Read(line.VariantId, 1, default);
            Assert.Equal(100 - line.Quantity, ledger.OnHand);
            Assert.Equal(0, ledger.Reserved);
            Assert.Equal(2, ledger.Total);
            var movement = Assert.Single(ledger.Items, x => x.Kind == "Consumption");
            Assert.Equal(-line.Quantity, movement.Quantity);
            Assert.Equal(r.Id, movement.Id);
            Assert.Equal(r.OrderId.ToString(), movement.Reference);
        }
    }

    [Fact]
    public async Task Wrong_order_and_unknown_reservation_never_mutate_stock()
    {
        var r = await Seed();
        await using var scope = fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IStockReservations>();
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => repo.Consume(r.Id, Guid.NewGuid(), default))).Status);
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => repo.Consume(Guid.NewGuid(), r.OrderId, default))).Status);
        Assert.Equal(400, (await Assert.ThrowsAsync<BusinessException>(() => repo.Consume(r.Id, Guid.Empty, default))).Status);
        foreach (var line in r.Lines)
        {
            var ledger = await scope.ServiceProvider.GetRequiredService<IStockLedger>().Read(line.VariantId, 1, default);
            Assert.Equal(100, ledger.OnHand);
            Assert.Equal(line.Quantity, ledger.Reserved);
            Assert.Equal(1, ledger.Total);
        }
    }

    [Fact]
    public async Task Consume_racing_release_has_one_terminal_outcome_without_restocking()
    {
        var r = await Seed();
        async Task<StockReservation> Release()
        {
            await using var scope = fixture.Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<IStockReservations>().Release(r.Id, r.OrderId, default);
        }

        var results = await Task.WhenAll(Consume(r), Release());
        Assert.Equal(results[0].Status, results[1].Status);
        var consumed = results[0].Status == ReservationStatus.Consumed;
        await using var scope = fixture.Services.CreateAsyncScope();
        foreach (var line in r.Lines)
        {
            var ledger = await scope.ServiceProvider.GetRequiredService<IStockLedger>().Read(line.VariantId, 1, default);
            Assert.Equal(consumed ? 100 - line.Quantity : 100, ledger.OnHand);
            Assert.Equal(0, ledger.Reserved);
            Assert.Equal(consumed ? 2 : 1, ledger.Total);
        }
    }

    [Fact]
    public async Task Expired_reservation_is_released_without_consumption_even_with_concurrent_expiry()
    {
        var r = await Seed();
        await using var scope = fixture.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<InventoryDbContext>().Reservations.Where(x => x.Id == r.Id).ExecuteUpdateAsync(x => x.SetProperty(v => v.ExpiresAt, DateTimeOffset.UtcNow.AddSeconds(-1)));
        await Task.WhenAll(Consume(r), scope.ServiceProvider.GetRequiredService<IStockReservations>().Expire(100, default));
        Assert.Equal(ReservationStatus.Expired, (await Consume(r)).Status);
        foreach (var line in r.Lines)
        {
            var ledger = await scope.ServiceProvider.GetRequiredService<IStockLedger>().Read(line.VariantId, 1, default);
            Assert.Equal(100, ledger.OnHand);
            Assert.Equal(0, ledger.Reserved);
            Assert.Equal(1, ledger.Total);
        }
    }

    [Fact]
    public async Task Later_balance_failure_rolls_back_earlier_balance_status_and_ledger()
    {
        var r = await Seed();
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var last = r.Lines.Last();
        await db.Balances.Where(x => x.VariantId == last.VariantId).ExecuteUpdateAsync(x => x.SetProperty(v => v.Reserved, 0));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Consume(r));
        Assert.Equal(ReservationStatus.Active, (await db.Reservations.AsNoTracking().SingleAsync(x => x.Id == r.Id)).Status);
        Assert.False(await db.Consumptions.AnyAsync(x => x.ReservationId == r.Id));
        var first = await db.Balances.AsNoTracking().SingleAsync(x => x.VariantId == r.Lines.First().VariantId);
        Assert.Equal(100, first.OnHand);
        Assert.Equal(r.Lines.First().Quantity, first.Reserved);
        await db.Balances.Where(x => x.VariantId == last.VariantId).ExecuteUpdateAsync(x => x.SetProperty(v => v.Reserved, last.Quantity));
        Assert.Equal(ReservationStatus.Consumed, (await Consume(r)).Status);
    }
}
