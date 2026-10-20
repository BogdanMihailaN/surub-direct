using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Application;
using Xunit;
namespace SurubDirect.Tests;
public sealed class PurchaseOrderPersistenceTests(PurchasingFixture fixture) : IClassFixture<PurchasingFixture>
{
    [Theory]
    [InlineData(true, 409)]
    [InlineData(false, 503)]
    public async Task Catalog_failure_rolls_back_lines_and_does_not_mark_orders_sent(bool missing, int status)
    {
        await using var scope = fixture.Services.CreateAsyncScope(); var s = scope.ServiceProvider;
        var supplier = await s.GetRequiredService<CreateSupplierHandler>().Handle(new("Dependency test", "RO", Guid.NewGuid().ToString("N"), "", "", "", "", ""), default);
        var item = await s.GetRequiredService<AddSupplierItemHandler>().Handle(supplier, new(Guid.NewGuid(), "CODE", "Cutie", 10, 5, "RON"), default);
        var id = await s.GetRequiredService<CreatePurchaseOrderHandler>().Handle(new(supplier, "RON", ""), default);
        var repo = s.GetRequiredService<IPurchaseOrderRepository>(); var catalog = new UnavailablePurchasingCatalog(missing);
        var error = await Assert.ThrowsAsync<BusinessException>(() => new AddPurchaseOrderLineHandler(repo, catalog).Handle(id, new(1, item, 2), default));
        Assert.Equal(status, error.Status);
        var order = await repo.Get(id, default); Assert.NotNull(order); Assert.Empty(order.Lines); Assert.Equal(1, order.Version);
        await s.GetRequiredService<AddPurchaseOrderLineHandler>().Handle(id, new(1, item, 2), default);
        error = await Assert.ThrowsAsync<BusinessException>(() => new MarkPurchaseOrderSentHandler(repo, catalog).Handle(id, new(2), default));
        Assert.Equal(status, error.Status);
        order = await repo.Get(id, default); Assert.NotNull(order); Assert.Equal("Draft", order.Status); Assert.Null(order.SentAt); Assert.Equal(2, order.Version);
    }
    [Fact]
    public async Task Orders_persist_snapshots_remove_lines_and_reject_concurrent_stale_writes()
    {
        Guid id; long number;
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var s = scope.ServiceProvider;
            var supplier = await s.GetRequiredService<CreateSupplierHandler>().Handle(new("Order supplier", "RO", Guid.NewGuid().ToString("N"), "", "", "", "", ""), default);
            var item = await s.GetRequiredService<AddSupplierItemHandler>().Handle(supplier, new(Guid.NewGuid(), "CODE", "Cutie", 100, 12.5m, "RON"), default);
            id = await s.GetRequiredService<CreatePurchaseOrderHandler>().Handle(new(supplier, "RON", ""), default);
            await s.GetRequiredService<AddPurchaseOrderLineHandler>().Handle(id, new(1, item, 2), default);
            await s.GetRequiredService<UpdateSupplierItemHandler>().Handle(supplier, item, new("NEW", "Punga", 10, 99, "EUR", true), default);
            var order = await s.GetRequiredService<GetPurchaseOrderHandler>().Handle(id, default);
            number = order.Number; Assert.True(number > 0); Assert.Equal(25m, order.NetTotal); Assert.Equal(200, Assert.Single(order.Lines).BaseUnits);
        }
        async Task<bool> Edit()
        {
            await using var scope = fixture.Services.CreateAsyncScope();
            try { await scope.ServiceProvider.GetRequiredService<UpdatePurchaseOrderHandler>().Handle(id, new(2, "Concurrent"), default); return true; }
            catch (BusinessException e) { Assert.Equal(409, e.Status); return false; }
        }
        Assert.Single(await Task.WhenAll(Edit(), Edit()), x => x);
        await using var check = fixture.Services.CreateAsyncScope(); var services = check.ServiceProvider;
        var saved = await services.GetRequiredService<GetPurchaseOrderHandler>().Handle(id, default);
        Assert.Equal(3, saved.Version);
        var found = await services.GetRequiredService<ListPurchaseOrdersHandler>().Handle("CA-" + number, "Draft", 1, default);
        Assert.Equal(id, Assert.Single(found.Items).Id);
        await services.GetRequiredService<RemovePurchaseOrderLineHandler>().Handle(id, saved.Lines[0].Id, new(3), default);
        Assert.Empty((await services.GetRequiredService<GetPurchaseOrderHandler>().Handle(id, default)).Lines);
        await Assert.ThrowsAsync<BusinessException>(() => services.GetRequiredService<MarkPurchaseOrderSentHandler>().Handle(id, new(4), default));
        Assert.Equal(4, (await services.GetRequiredService<GetPurchaseOrderHandler>().Handle(id, default)).Version);
    }
}
