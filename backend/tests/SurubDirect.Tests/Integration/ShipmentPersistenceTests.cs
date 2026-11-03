using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Infrastructure;
using Xunit;
namespace SurubDirect.Tests;
public sealed class ShipmentPersistenceTests(OrderingCartFixture fixture) : IClassFixture<OrderingCartFixture>
{
    private async Task<Guid> Seed()
    {
        var (order, plan) = PaymentSimulationSamples.Create();
        var now = DateTimeOffset.UtcNow;
        order.ChangeProcessing(Guid.NewGuid(), 0, "Processing", "Prepare order", "admin", now);
        order.ChangeProcessing(Guid.NewGuid(), 1, "Ready", "Prepare order", "admin", now);
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        db.Add(order); db.Add(plan); await db.SaveChangesAsync();
        return order.Id;
    }
    private async Task Change(Guid id, Guid operation, long version, string target, string actor = "admin")
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ChangeShipmentHandler>().Handle(id, operation, version, target, "Test shipment", actor, default);
    }
    private async Task Pay(Guid id, long version, string action)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SimulatePaymentHandler>().Handle(id, Guid.NewGuid(), version, action, "Test payment", "admin", default);
    }
    [Fact]
    public async Task Replay_is_deduplicated_and_refunding_a_shipped_order_never_restores_stock()
    {
        var id = await Seed(); var operation = Guid.NewGuid();
        await Task.WhenAll(Change(id, operation, 0, "Ready"), Change(id, operation, 0, "Ready"));
        await Change(id, operation, 0, "Ready", "other-admin");
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => Change(id, operation, 0, "Delivered"))).Status);
        await Assert.ThrowsAsync<BusinessException>(() => Change(id, Guid.NewGuid(), 1, "Shipped"));
        await Pay(id, 0, "Start"); await Pay(id, 1, "Succeed");
        await Change(id, Guid.NewGuid(), 1, "Shipped");
        await Pay(id, 2, "Refund");
        await using var scope = fixture.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        await Assert.ThrowsAsync<BusinessException>(() => services.GetRequiredService<IOrderCancellations>().Cancel(id, "Cancel after refund", "admin", default));
        var delivered = Guid.NewGuid();
        await Task.WhenAll(Change(id, delivered, 2, "Delivered"), Change(id, delivered, 2, "Delivered"));
        await Change(id, operation, 0, "Ready");
        var detail = (await services.GetRequiredService<IAdminDemoOrders>().Find(id, default))!;
        Assert.Equal("Delivered", detail.Shipment.Status);
        Assert.Equal(3, detail.Shipment.History.Count);
        Assert.Equal("admin", detail.Shipment.History[0].Actor);
        Assert.Equal("Confirmed", detail.Order.Status);
        Assert.Equal("Consumed", detail.Reservation.Status);
        Assert.Equal("Refunded", detail.PaymentSimulation.Status);
        var summary = Assert.Single((await services.GetRequiredService<IDemoOrders>().Read(new string('a', 64), 1, default)).Items, x => x.Id == id);
        Assert.Equal(detail.Shipment.Reference, summary.Shipment.Reference);
        Assert.Equal("Delivered", summary.Shipment.Status);
    }
    [Fact]
    public async Task Refund_and_dispatch_use_the_same_lock_and_never_dispatch_after_a_refund()
    {
        var id = await Seed(); await Change(id, Guid.NewGuid(), 0, "Ready");
        await Pay(id, 0, "Start"); await Pay(id, 1, "Succeed");
        async Task<bool> Dispatch()
        {
            try { await Change(id, Guid.NewGuid(), 1, "Shipped"); return true; }
            catch (BusinessException e) when (e.Status == 409) { return false; }
        }
        var dispatch = Dispatch(); var refund = Pay(id, 2, "Refund");
        await Task.WhenAll(dispatch, refund);
        await using var scope = fixture.Services.CreateAsyncScope();
        var detail = (await scope.ServiceProvider.GetRequiredService<IAdminDemoOrders>().Find(id, default))!;
        Assert.Equal(await dispatch ? "Shipped" : "Ready", detail.Shipment.Status);
        Assert.Equal("Refunded", detail.PaymentSimulation.Status);
        Assert.Equal("Consumed", detail.Reservation.Status);
        if (!await dispatch) await Assert.ThrowsAsync<BusinessException>(() => Change(id, Guid.NewGuid(), 1, "Shipped"));
    }
    [Fact]
    public async Task Cancelled_order_cannot_create_a_shipment_or_leave_an_event()
    {
        var id = await Seed();
        await using var scope = fixture.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IOrderCancellations>().Cancel(id, "Cancel before shipping", "admin", default);
        await Assert.ThrowsAsync<BusinessException>(() => Change(id, Guid.NewGuid(), 0, "Ready"));
        Assert.False(await scope.ServiceProvider.GetRequiredService<OrderingDbContext>().ShipmentChanges.AnyAsync(x => x.OrderId == id));
    }
}
