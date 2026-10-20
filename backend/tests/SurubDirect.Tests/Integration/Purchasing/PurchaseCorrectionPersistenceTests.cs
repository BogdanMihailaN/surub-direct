using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Application;
using Xunit;
namespace SurubDirect.Tests;

public sealed class PurchaseCorrectionPersistenceTests(PurchasingFixture fixture) : IClassFixture<PurchasingFixture>
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
    public async Task Pending_correction_holds_capacity_and_retry_releases_it_once()
    {
        await using var scope = fixture.Services.CreateAsyncScope(); var s = scope.ServiceProvider; var (id, line) = await Create(s);
        var receipts = s.GetRequiredService<IPurchaseReceiptRepository>(); var receiptId = Guid.NewGuid();
        var receive = new ReceivePurchaseOrderHandler(receipts, new RecordingInventoryReceipts());
        await receive.Handle(id, new(receiptId, 3, line, 200, "REC"), "admin", default);
        var repo = s.GetRequiredService<IPurchaseCorrectionRepository>(); var inventory = new RecordingInventoryCorrections { LoseNextReply = true };
        var handler = new CorrectPurchaseReceiptHandler(repo, inventory); var command = new CorrectPurchaseReceipt(Guid.NewGuid(), 5, receiptId, 30, "COR", "Wrong count");
        await Assert.ThrowsAsync<BusinessException>(() => handler.Handle(id, command, "admin", default));
        var pending = await s.GetRequiredService<GetPurchaseOrderHandler>().Handle(id, default);
        Assert.Null(Assert.Single(pending.Corrections).AppliedAt); Assert.Equal(6, pending.Version);
        await Assert.ThrowsAsync<BusinessException>(() => receive.Handle(id, new(Guid.NewGuid(), 6, line, 1, "REC2"), "admin", default));
        await Assert.ThrowsAsync<BusinessException>(() => handler.Handle(id, new(Guid.NewGuid(), 6, receiptId, 171, "COR2", "Wrong count"), "admin", default));
        await new RetryPurchaseCorrectionHandler(repo, inventory).Handle(id, command.RequestId, default);
        await handler.Handle(id, command, "admin", default);
        Assert.Equal(30, Assert.Single(inventory.Corrections).Value);
        var applied = await s.GetRequiredService<GetPurchaseOrderHandler>().Handle(id, default); Assert.Equal(7, applied.Version); Assert.NotNull(Assert.Single(applied.Corrections).AppliedAt); Assert.Equal(200, Assert.Single(applied.Receipts).Quantity);
        await Assert.ThrowsAsync<BusinessException>(() => handler.Handle(id, command with { Quantity = 31 }, "admin", default));
        await receive.Handle(id, new(Guid.NewGuid(), 7, line, 30, "REC2"), "admin", default);
        var final = await s.GetRequiredService<GetPurchaseOrderHandler>().Handle(id, default);
        Assert.Equal(200, final.Receipts.Sum(r => r.Quantity) - final.Corrections.Sum(c => c.Quantity));
    }
    [Fact]
    public async Task Pending_original_receipt_cannot_be_corrected()
    {
        await using var scope = fixture.Services.CreateAsyncScope(); var s = scope.ServiceProvider; var (id, line) = await Create(s); var receiptId = Guid.NewGuid();
        await s.GetRequiredService<IPurchaseReceiptRepository>().Reserve(id, new(receiptId, 3, line, 50, "REC"), "admin", default);
        var e = await Assert.ThrowsAsync<BusinessException>(() => s.GetRequiredService<IPurchaseCorrectionRepository>().Reserve(id, new(Guid.NewGuid(), 4, receiptId, 1, "COR", "Count"), "admin", default)); Assert.Equal(409, e.Status);
    }
    [Fact]
    public async Task Concurrent_corrections_serialize_and_identical_retries_share_one_intent()
    {
        Guid id, line, receiptId = Guid.NewGuid();
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var s = scope.ServiceProvider; (id, line) = await Create(s);
            await new ReceivePurchaseOrderHandler(s.GetRequiredService<IPurchaseReceiptRepository>(), new RecordingInventoryReceipts()).Handle(id, new(receiptId, 3, line, 50, "REC"), "admin", default);
        }
        var command = new CorrectPurchaseReceipt(Guid.NewGuid(), 5, receiptId, 40, "COR", "Count");
        async Task<bool> Reserve(CorrectPurchaseReceipt c)
        {
            await using var scope = fixture.Services.CreateAsyncScope();
            try { await scope.ServiceProvider.GetRequiredService<IPurchaseCorrectionRepository>().Reserve(id, c, "admin", default); return true; }
            catch (BusinessException e) { Assert.Equal(409, e.Status); return false; }
        }
        Assert.All(await Task.WhenAll(Reserve(command), Reserve(command)), Assert.True);
        Assert.False(await Reserve(command with { RequestId = Guid.NewGuid(), Version = 6, Quantity = 11, Reference = "COR2" }));
        await using var check = fixture.Services.CreateAsyncScope();
        Assert.Equal(40, Assert.Single((await check.ServiceProvider.GetRequiredService<GetPurchaseOrderHandler>().Handle(id, default)).Corrections).Quantity);
    }
}
