using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Inventory.Application;
using SurubDirect.Inventory.Domain;
using SurubDirect.Inventory.Infrastructure;
using Xunit;
namespace SurubDirect.Tests;
public sealed class StockAdjustmentTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task Ledger_preserves_receipts_and_duplicate_adjustments_change_stock_once()
    {
        var variant = Guid.NewGuid(); var command = StockAdjustment.Create(Guid.NewGuid(), variant, -20, 100, 0, "INV-1", "Inventar fizic", "admin");
        await using (var s = fixture.Services.CreateAsyncScope())
            await s.ServiceProvider.GetRequiredService<IInventoryRepository>().Receive(StockReceipt.Create(Guid.NewGuid(),variant,100,"REC","admin"),default);
        async Task<bool> Adjust() { await using var s = fixture.Services.CreateAsyncScope(); return await s.ServiceProvider.GetRequiredService<IStockLedger>().Adjust(command,default); }
        Assert.Single(await Task.WhenAll(Adjust(),Adjust()),x=>x);
        await using var check = fixture.Services.CreateAsyncScope(); var ledger = check.ServiceProvider.GetRequiredService<IStockLedger>();
        var result = await ledger.Read(variant,1,default); Assert.Equal(80,result.OnHand); Assert.Equal(2,result.Total);
        Assert.Contains(result.Items,x=>x.Kind=="Receipt" && x.Quantity==100);
        Assert.Contains(result.Items,x=>x.Kind=="Adjustment" && x.Quantity==-20 && x.Reason=="Inventar fizic");
        await Assert.ThrowsAsync<BusinessException>(()=>ledger.Adjust(StockAdjustment.Create(command.Id,variant,-21,100,0,"INV-1","Inventar fizic","admin"),default));
        Assert.Equal(80,(await ledger.Read(variant,1,default)).OnHand);
    }
    [Fact]
    public async Task Concurrent_adjustments_and_reserved_stock_are_protected_and_failures_roll_back()
    {
        var variant=Guid.NewGuid();
        await using(var s=fixture.Services.CreateAsyncScope()) {
            var db=s.ServiceProvider.GetRequiredService<InventoryDbContext>();db.Balances.Add(new(){VariantId=variant,OnHand=100,Reserved=30});await db.SaveChangesAsync();
        }
        async Task<bool> Adjust() {
            await using var s=fixture.Services.CreateAsyncScope();
            try { return await s.ServiceProvider.GetRequiredService<IStockLedger>().Adjust(StockAdjustment.Create(Guid.NewGuid(),variant,-60,100,30,"INV","Numărare","admin"),default); }
            catch(BusinessException e){Assert.Equal(409,e.Status);return false;}
        }
        Assert.Single(await Task.WhenAll(Adjust(),Adjust()),x=>x);
        await using var check=fixture.Services.CreateAsyncScope();var ledger=check.ServiceProvider.GetRequiredService<IStockLedger>();
        await Assert.ThrowsAsync<BusinessException>(()=>ledger.Adjust(StockAdjustment.Create(Guid.NewGuid(),variant,-11,40,30,"INV-2","Numărare","admin"),default));
        var result=await ledger.Read(variant,1,default);Assert.Equal(40,result.OnHand);Assert.Equal(30,result.Reserved);Assert.Single(result.Items);
        await ledger.Adjust(StockAdjustment.Create(Guid.NewGuid(),variant,10,40,30,"INV-3","Găsite","admin"),default);
        Assert.Equal(50,(await ledger.Read(variant,1,default)).OnHand);
    }
    [Fact]
    public async Task Empty_stock_cannot_become_negative_and_history_is_paginated()
    {
        await using var scope=fixture.Services.CreateAsyncScope();var ledger=scope.ServiceProvider.GetRequiredService<IStockLedger>();var variant=Guid.NewGuid();
        await Assert.ThrowsAsync<BusinessException>(()=>ledger.Adjust(StockAdjustment.Create(Guid.NewGuid(),variant,-1,0,0,"NEG","Test","admin"),default));
        Assert.Equal(0,(await ledger.Read(variant,1,default)).Total);
        for(var i=0;i<26;i++) await ledger.Adjust(StockAdjustment.Create(Guid.NewGuid(),variant,1,i,0,"INV-"+i,"Inventar","admin"),default);
        var first=await ledger.Read(variant,1,default);var second=await ledger.Read(variant,2,default);
        Assert.Equal(26,first.OnHand);Assert.Equal(26,first.Total);Assert.Equal(25,first.Items.Count);Assert.Single(second.Items);
        Assert.DoesNotContain(first.Items,x=>x.Id==second.Items[0].Id);
    }
}
