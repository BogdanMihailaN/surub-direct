using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Inventory.Application;
using SurubDirect.Inventory.Domain;
using SurubDirect.Inventory.Infrastructure;
using Xunit;
namespace SurubDirect.Tests;

public sealed class ReceiptCorrectionTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task Concurrent_retries_decrement_once_and_keep_original_receipt()
    {
        var variant = Guid.NewGuid(); var receipt = StockReceipt.Create(Guid.NewGuid(), variant, 100, "REC", "admin");
        await using (var s = fixture.Services.CreateAsyncScope()) await s.ServiceProvider.GetRequiredService<IInventoryRepository>().Receive(receipt, default);
        var correction = ReceiptCorrection.Create(Guid.NewGuid(), receipt.Id, variant, 30, "COR", "Wrong count", "admin");
        async Task<bool> Apply() { await using var s = fixture.Services.CreateAsyncScope(); return await s.ServiceProvider.GetRequiredService<IReceiptCorrections>().Apply(correction, default); }
        Assert.Single(await Task.WhenAll(Apply(), Apply()), x => x);
        await using var check = fixture.Services.CreateAsyncScope(); var repo = check.ServiceProvider.GetRequiredService<IReceiptCorrections>();
        var ledger = await check.ServiceProvider.GetRequiredService<IStockLedger>().Read(variant, 1, default);
        Assert.Equal(70, ledger.OnHand); Assert.Equal(2, ledger.Total);
        Assert.Contains(ledger.Items, x => x.Kind == "Correction" && x.Quantity == -30 && x.OriginalReceiptId == receipt.Id);
        Assert.Contains(ledger.Items, x => x.Kind == "Receipt" && x.Quantity == 100);
        await Assert.ThrowsAsync<BusinessException>(() => repo.Apply(ReceiptCorrection.Create(correction.Id, receipt.Id, variant, 31, "COR", "Wrong count", "admin"), default));
        await Assert.ThrowsAsync<BusinessException>(() => repo.Apply(ReceiptCorrection.Create(Guid.NewGuid(), receipt.Id, variant, 71, "COR2", "Wrong count", "admin"), default));
        await Assert.ThrowsAsync<BusinessException>(() => repo.Apply(ReceiptCorrection.Create(Guid.NewGuid(), receipt.Id, Guid.NewGuid(), 1, "COR3", "Wrong count", "admin"), default));
    }
    [Fact]
    public async Task Reserved_stock_failure_rolls_back_and_same_intent_can_be_retried()
    {
        await using var s = fixture.Services.CreateAsyncScope(); var services = s.ServiceProvider;
        var variant = Guid.NewGuid(); var receipt = StockReceipt.Create(Guid.NewGuid(), variant, 100, "REC", "admin");
        await services.GetRequiredService<IInventoryRepository>().Receive(receipt, default);
        var db = services.GetRequiredService<InventoryDbContext>();
        await db.Balances.Where(x => x.VariantId == variant).ExecuteUpdateAsync(u => u.SetProperty(x => x.Reserved, 80));
        var c = ReceiptCorrection.Create(Guid.NewGuid(), receipt.Id, variant, 30, "COR", "Wrong count", "admin"); var repo = services.GetRequiredService<IReceiptCorrections>();
        await Assert.ThrowsAsync<BusinessException>(() => repo.Apply(c, default));
        Assert.False(await db.ReceiptCorrections.AnyAsync(x => x.Id == c.Id));
        Assert.Equal(100, (await db.Balances.AsNoTracking().SingleAsync(x => x.VariantId == variant)).OnHand);
        await db.Balances.Where(x => x.VariantId == variant).ExecuteUpdateAsync(u => u.SetProperty(x => x.Reserved, 0));
        Assert.True(await repo.Apply(c, default));
        Assert.Equal(70, (await db.Balances.AsNoTracking().SingleAsync(x => x.VariantId == variant)).OnHand);
    }
}
