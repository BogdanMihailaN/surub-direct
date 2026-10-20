using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Application;
using SurubDirect.Purchasing.Domain;
using Xunit;
namespace SurubDirect.Tests;

public sealed class PurchaseOrderClosurePersistenceTests(PurchasingFixture fixture) : IClassFixture<PurchasingFixture>
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
    public async Task Closure_persists_audit_and_closed_filter_without_changing_ordered_units()
    {
        await using var scope = fixture.Services.CreateAsyncScope(); var s = scope.ServiceProvider; var (id, line) = await Create(s);
        var close = s.GetRequiredService<ClosePurchaseOrderHandler>();
        await Assert.ThrowsAsync<BusinessException>(() => close.Handle(id, new(2, "No delivery"), "admin", default));
        await close.Handle(id, new(3, "No delivery"), "admin", default);
        var order = await s.GetRequiredService<GetPurchaseOrderHandler>().Handle(id, default);
        Assert.Equal("Closed", order.Status); Assert.Equal(4, order.Version); Assert.Equal("admin", order.ClosedBy); Assert.Equal("No delivery", order.ClosureReason); Assert.NotNull(order.ClosedAt); Assert.Equal(200, Assert.Single(order.Lines).BaseUnits);
        var list = await s.GetRequiredService<IPurchaseOrderRepository>().List(order.Number.ToString(), PurchaseOrderStatus.Closed, 1, default); Assert.Contains(list.Items, o => o.Id == id);
        await Assert.ThrowsAsync<BusinessException>(() => s.GetRequiredService<IPurchaseReceiptRepository>().Reserve(id, new(Guid.NewGuid(), 4, line, 1, "REC"), "admin", default));
        await Assert.ThrowsAsync<BusinessException>(() => s.GetRequiredService<CancelPurchaseOrderHandler>().Handle(id, new(4, "Cancel"), default));
    }
    [Fact]
    public async Task Concurrent_receipt_and_closure_cannot_both_succeed()
    {
        Guid id, line; await using (var scope = fixture.Services.CreateAsyncScope()) (id, line) = await Create(scope.ServiceProvider);
        async Task<bool> Close() { await using var scope = fixture.Services.CreateAsyncScope(); try { await scope.ServiceProvider.GetRequiredService<ClosePurchaseOrderHandler>().Handle(id, new(3, "No more delivery"), "admin", default); return true; } catch (BusinessException e) { Assert.Equal(409, e.Status); return false; } }
        async Task<bool> Receive() { await using var scope = fixture.Services.CreateAsyncScope(); try { await scope.ServiceProvider.GetRequiredService<IPurchaseReceiptRepository>().Reserve(id, new(Guid.NewGuid(), 3, line, 10, "REC"), "admin", default); return true; } catch (BusinessException e) { Assert.Equal(409, e.Status); return false; } }
        Assert.Single(await Task.WhenAll(Close(), Receive()), x => x);
        await using var check = fixture.Services.CreateAsyncScope(); var s = check.ServiceProvider; var o = await s.GetRequiredService<GetPurchaseOrderHandler>().Handle(id, default);
        if (o.Status == "Closed") Assert.Empty(o.Receipts);
        else { Assert.Null(Assert.Single(o.Receipts).AppliedAt); await Assert.ThrowsAsync<BusinessException>(() => s.GetRequiredService<ClosePurchaseOrderHandler>().Handle(id, new(o.Version, "Close"), "admin", default)); }
    }
}
