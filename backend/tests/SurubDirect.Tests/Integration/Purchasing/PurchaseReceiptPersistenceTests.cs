using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Application;
using Xunit;
namespace SurubDirect.Tests;
public sealed class PurchaseReceiptPersistenceTests(PurchasingFixture fixture) : IClassFixture<PurchasingFixture>
{
    private static async Task<(Guid Order, Guid Line)> Create(IServiceProvider s)
    {
        var supplier = await s.GetRequiredService<CreateSupplierHandler>().Handle(new("Receipt supplier", "RO", Guid.NewGuid().ToString("N"), "", "", "", "", ""), default);
        var item = await s.GetRequiredService<AddSupplierItemHandler>().Handle(supplier, new(Guid.NewGuid(), "CODE", "Cutie", 100, 10, "RON"), default);
        var id = await s.GetRequiredService<CreatePurchaseOrderHandler>().Handle(new(supplier, "RON", ""), default);
        await s.GetRequiredService<AddPurchaseOrderLineHandler>().Handle(id, new(1, item, 2), default);
        await s.GetRequiredService<MarkPurchaseOrderSentHandler>().Handle(id, new(2), default);
        var order = await s.GetRequiredService<GetPurchaseOrderHandler>().Handle(id, default);
        return (id, order.Lines[0].Id);
    }
    [Fact]
    public async Task Simultaneous_identical_requests_share_one_persistent_receipt()
    {
        Guid id, line; await using (var scope = fixture.Services.CreateAsyncScope()) (id, line) = await Create(scope.ServiceProvider);
        var command = new ReceivePurchaseOrder(Guid.NewGuid(), 3, line, 50, "Same-document");
        async Task<Guid> Reserve()
        {
            await using var scope = fixture.Services.CreateAsyncScope();
            return (await scope.ServiceProvider.GetRequiredService<IPurchaseReceiptRepository>().Reserve(id, command, "admin", default)).Id;
        }
        Assert.All(await Task.WhenAll(Reserve(), Reserve()), result => Assert.Equal(command.RequestId, result));
        await using var check = fixture.Services.CreateAsyncScope();
        Assert.Equal(50, Assert.Single((await check.ServiceProvider.GetRequiredService<GetPurchaseOrderHandler>().Handle(id, default)).Receipts).Quantity);
    }
    [Fact]
    public async Task Lost_reply_preserves_pending_quantity_and_retries_do_not_duplicate_stock()
    {
        await using var scope = fixture.Services.CreateAsyncScope(); var s = scope.ServiceProvider;
        var (id, line) = await Create(s); var repo = s.GetRequiredService<IPurchaseReceiptRepository>();
        var inventory = new RecordingInventoryReceipts { LoseNextReply = true };
        var handler = new ReceivePurchaseOrderHandler(repo, inventory);
        var command = new ReceivePurchaseOrder(Guid.NewGuid(), 3, line, 120, "Aviz-1");
        await Assert.ThrowsAsync<BusinessException>(() => handler.Handle(id, command, "admin", default));
        var pending = await s.GetRequiredService<GetPurchaseOrderHandler>().Handle(id, default);
        Assert.Null(Assert.Single(pending.Receipts).AppliedAt); Assert.Equal(4, pending.Version);
        await Assert.ThrowsAsync<BusinessException>(() => handler.Handle(id, new(Guid.NewGuid(), 4, line, 81, "Aviz-2"), "admin", default));
        await Assert.ThrowsAsync<BusinessException>(() => s.GetRequiredService<CancelPurchaseOrderHandler>().Handle(id, new(4, "Cancel"), default));
        await new RetryPurchaseReceiptHandler(repo, inventory).Handle(id, command.RequestId, default);
        await handler.Handle(id, command, "admin", default); // Original stale version is safe for an identical retry.
        Assert.Equal(120, Assert.Single(inventory.Receipts).Value);
        var applied = await s.GetRequiredService<GetPurchaseOrderHandler>().Handle(id, default);
        Assert.NotNull(Assert.Single(applied.Receipts).AppliedAt); Assert.Equal(5, applied.Version);
        await Assert.ThrowsAsync<BusinessException>(() => handler.Handle(id, command with { Quantity = 119 }, "admin", default));
        await Assert.ThrowsAsync<BusinessException>(() => handler.Handle(id, command with { RequestId = Guid.NewGuid(), Version = 5 }, "admin", default));
        await handler.Handle(id, new(Guid.NewGuid(), 5, line, 80, "Aviz-2"), "admin", default);
        Assert.Equal(200, inventory.Receipts.Values.Sum());
        var final = await s.GetRequiredService<GetPurchaseOrderHandler>().Handle(id, default);
        Assert.Equal(2, final.Receipts.Count); Assert.All(final.Receipts, r => Assert.NotNull(r.AppliedAt));
        await Assert.ThrowsAsync<BusinessException>(() => handler.Handle(id, new(Guid.NewGuid(), final.Version, line, 1, "Aviz-3"), "admin", default));
    }
    [Fact]
    public async Task Concurrent_receipts_cannot_exceed_the_ordered_quantity()
    {
        Guid id, line; await using (var scope = fixture.Services.CreateAsyncScope()) (id, line) = await Create(scope.ServiceProvider);
        async Task<bool> Reserve()
        {
            await using var scope = fixture.Services.CreateAsyncScope();
            try { await scope.ServiceProvider.GetRequiredService<IPurchaseReceiptRepository>().Reserve(id, new(Guid.NewGuid(), 3, line, 150, Guid.NewGuid().ToString()), "admin", default); return true; }
            catch (BusinessException e) { Assert.Equal(409, e.Status); return false; }
        }
        Assert.Single(await Task.WhenAll(Reserve(), Reserve()), x => x);
        await using var check = fixture.Services.CreateAsyncScope();
        Assert.Equal(150, Assert.Single((await check.ServiceProvider.GetRequiredService<GetPurchaseOrderHandler>().Handle(id, default)).Receipts).Quantity);
    }
}
