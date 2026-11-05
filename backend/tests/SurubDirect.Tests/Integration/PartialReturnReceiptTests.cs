using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Inventory.Application;
using SurubDirect.Inventory.Domain;
using SurubDirect.Inventory.Infrastructure;
using Xunit;
namespace SurubDirect.Tests;
public sealed class PartialReturnReceiptTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    private async Task<StockReservation> Seed(bool consume = true)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var variants = new[] { Guid.NewGuid(), Guid.NewGuid() }.Order().ToArray();
        var lines = new[] { new ReservationLine(variants[0], 20), new ReservationLine(variants[1], 30) };
        foreach (var line in lines)
            await services.GetRequiredService<IInventoryRepository>().Receive(StockReceipt.Create(Guid.NewGuid(), line.VariantId, 100, "INITIAL", "test"), default);
        var repo = services.GetRequiredService<IStockReservations>();
        var r = await repo.Reserve(StockReservation.Create(Guid.NewGuid(), Guid.NewGuid(), 120, lines, DateTimeOffset.UtcNow), default);
        return consume ? await repo.Consume(r.Id, r.OrderId, default) : r;
    }
    private static ReturnReceipt Receipt(StockReservation r, int quantity, Guid? id = null) => ReturnReceipt.Create(id ?? Guid.NewGuid(), r.Id, r.OrderId, [new(r.Lines[0].VariantId, quantity)], DateTimeOffset.UtcNow);
    private async Task<ReturnReceipt> Receive(ReturnReceipt receipt)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IReturnReceipts>().Receive(receipt, default);
    }
    [Fact]
    public async Task Concurrent_replay_preserves_receipt_date_and_increments_only_selected_quantity_once()
    {
        var r = await Seed();
        var receipt = Receipt(r, 7);
        var results = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => Receive(Receipt(r, 7, receipt.Id))));
        Assert.All(results, x => Assert.Equal(results[0].CreatedAt, x.CreatedAt));
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        Assert.Single(await db.ReturnReceipts.Where(x => x.ReservationId == r.Id).ToListAsync());
        Assert.Equal(ReservationStatus.Consumed, (await db.Reservations.SingleAsync(x => x.Id == r.Id)).Status);
        Assert.False(await db.ReservationOutbox.AnyAsync(x => x.ReservationId == r.Id && x.Status == "Restored"));
        var ledger = await scope.ServiceProvider.GetRequiredService<IStockLedger>().Read(r.Lines[0].VariantId, 1, default);
        Assert.Equal(87, ledger.OnHand); Assert.Equal(0, ledger.Reserved);
        Assert.Equal(7, Assert.Single(ledger.Items, x => x.Kind == "ReturnReceipt").Quantity);
        Assert.Equal(70, (await db.Balances.SingleAsync(x => x.VariantId == r.Lines[1].VariantId)).OnHand);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => Receive(Receipt(r, 8, receipt.Id)))).Status);
    }
    [Fact]
    public async Task Multiple_receipts_are_capped_by_original_consumption_and_full_restoration_is_fenced()
    {
        var r = await Seed();
        await Receive(Receipt(r, 7));
        await Receive(Receipt(r, 13));
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => Receive(Receipt(r, 1)))).Status);
        await using var scope = fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IStockReservations>();
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => repo.Restore(r.Id, r.OrderId, default))).Status);
        Assert.Equal(ReservationStatus.Consumed, (await repo.Consume(r.Id, r.OrderId, default)).Status);
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        Assert.Equal(100, (await db.Balances.SingleAsync(x => x.VariantId == r.Lines[0].VariantId)).OnHand);
        Assert.Equal(2, await db.ReturnReceipts.CountAsync(x => x.ReservationId == r.Id));
    }
    [Fact]
    public async Task Concurrent_distinct_receipts_cannot_over_receive()
    {
        var r = await Seed();
        async Task<bool> Attempt()
        {
            try { await Receive(Receipt(r, 15)); return true; }
            catch (BusinessException e) { Assert.Equal(409, e.Status); return false; }
        }
        var outcomes = await Task.WhenAll(Attempt(), Attempt());
        Assert.Single(outcomes, x => x);
        await using var scope = fixture.Services.CreateAsyncScope();
        Assert.Equal(95, (await scope.ServiceProvider.GetRequiredService<InventoryDbContext>().Balances.SingleAsync(x => x.VariantId == r.Lines[0].VariantId)).OnHand);
    }
    [Fact]
    public async Task Full_restoration_and_partial_receipt_race_has_only_one_winner()
    {
        var r = await Seed();
        async Task<bool> Full()
        {
            await using var scope = fixture.Services.CreateAsyncScope();
            try { await scope.ServiceProvider.GetRequiredService<IStockReservations>().Restore(r.Id, r.OrderId, default); return true; }
            catch (BusinessException e) { Assert.Equal(409, e.Status); return false; }
        }
        async Task<bool> Partial()
        {
            try { await Receive(Receipt(r, 7)); return true; }
            catch (BusinessException e) { Assert.Equal(409, e.Status); return false; }
        }
        Assert.Single(await Task.WhenAll(Full(), Partial()), x => x);
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var partial = await db.ReturnReceipts.AnyAsync(x => x.ReservationId == r.Id);
        Assert.Equal(partial ? 87 : 100, (await db.Balances.SingleAsync(x => x.VariantId == r.Lines[0].VariantId)).OnHand);
    }
    [Fact]
    public async Task Wrong_owner_unknown_sku_unconsumed_and_reused_receipt_are_rejected()
    {
        var r = await Seed();
        var other = await Seed();
        var active = await Seed(false);
        var receipt = Receipt(r, 3);
        await Receive(receipt);
        foreach (var candidate in new[] {
            ReturnReceipt.Create(Guid.NewGuid(), r.Id, Guid.NewGuid(), receipt.Lines, DateTimeOffset.UtcNow),
            ReturnReceipt.Create(Guid.NewGuid(), r.Id, r.OrderId, [new(Guid.NewGuid(), 1)], DateTimeOffset.UtcNow),
            Receipt(active, 1), Receipt(other, 3, receipt.Id) })
            Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => Receive(candidate))).Status);
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => Receive(ReturnReceipt.Create(Guid.NewGuid(), Guid.NewGuid(), r.OrderId, receipt.Lines, DateTimeOffset.UtcNow)))).Status);
    }
    [Fact]
    public async Task Late_balance_overflow_rolls_back_all_lines_and_same_receipt_can_retry()
    {
        var r = await Seed();
        var receipt = ReturnReceipt.Create(Guid.NewGuid(), r.Id, r.OrderId, r.Lines.Select(x => new ReturnReceiptLine(x.VariantId, 5)), DateTimeOffset.UtcNow);
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var last = r.Lines[1].VariantId;
        await db.Balances.Where(x => x.VariantId == last).ExecuteUpdateAsync(x => x.SetProperty(v => v.OnHand, int.MaxValue));
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => Receive(receipt))).Status);
        Assert.False(await db.ReturnReceipts.AnyAsync(x => x.Id == receipt.Id));
        Assert.Equal(80, (await db.Balances.SingleAsync(x => x.VariantId == r.Lines[0].VariantId)).OnHand);
        await db.Balances.Where(x => x.VariantId == last).ExecuteUpdateAsync(x => x.SetProperty(v => v.OnHand, 70));
        await Receive(receipt);
        db.ChangeTracker.Clear();
        Assert.Equal(85, (await db.Balances.SingleAsync(x => x.VariantId == r.Lines[0].VariantId)).OnHand);
        Assert.Equal(75, (await db.Balances.SingleAsync(x => x.VariantId == last)).OnHand);
    }
}
