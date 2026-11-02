using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Domain;
using SurubDirect.Ordering.Infrastructure;
using Xunit;

namespace SurubDirect.Tests;
public sealed class OrderProcessingTests(OrderingCartFixture fixture) : IClassFixture<OrderingCartFixture>
{
    private async Task<Guid> Seed(bool confirmed = true)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var now = DateTimeOffset.UtcNow;
        var cart = ShoppingCart.Create(new string('a', 64), now);
        var offer = Guid.NewGuid();
        var variant = Guid.NewGuid();
        var id = Guid.NewGuid();
        cart.Replace(cart.Id, cart.Version, [new("demo", offer, 2)], now);
        cart.SaveCustomer(cart.Id, cart.Version, CustomerDraftSamples.Person(), now);
        var snapshot = PreparedCart.Create(id, cart, [new(offer, variant, "Produs", "SKU", "Cutie", 2, 10, 10, 12.1m, 20, 24.2m)], "RON", now);
        var plan = ReservationPlan.Create(id, id, [new(variant, 20)], now);
        plan.Observe("Active", now.AddMinutes(15), now);
        plan.Confirm(now);
        var order = DemoOrder.Create(cart.OwnerHash, snapshot, now);
        if (confirmed) { plan.Observe("Consumed", now.AddMinutes(15), now); order.Reconcile(plan, now); }
        db.DemoOrders.Add(order);
        db.ReservationPlans.Add(plan);
        await db.SaveChangesAsync();
        return id;
    }

    private async Task Change(Guid id, Guid operation, long version, string target, string reason = "Verificare produse", string actor = "admin")
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ChangeOrderProcessingHandler>().Handle(id, operation, version, target, reason, actor, default);
    }

    [Fact]
    public async Task Concurrent_replays_keep_one_audit_and_stale_changes_cannot_overwrite_it()
    {
        var id = await Seed();
        var op = Guid.NewGuid();
        await Task.WhenAll(Change(id, op, 0, "Processing"), Change(id, op, 0, "Processing"));
        await Change(id, op, 0, "Processing", actor: "other-admin");
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => Change(id, op, 0, "Ready"))).Status);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => Change(id, Guid.NewGuid(), 0, "Ready"))).Status);
        await Change(id, Guid.NewGuid(), 1, "Ready");
        await Change(id, Guid.NewGuid(), 2, "Processing", "Reverificare ambalare");
        await using var scope = fixture.Services.CreateAsyncScope();
        var detail = (await scope.ServiceProvider.GetRequiredService<IAdminDemoOrders>().Find(id, default))!;
        Assert.Equal("Processing", detail.Processing.Status);
        Assert.Equal(3, detail.Processing.Version);
        Assert.Equal(3, detail.Processing.History.Count);
        Assert.Equal("admin", detail.Processing.History[0].Actor);
        Assert.Equal(new long[] { 1, 2, 3 }, detail.Processing.History.Select(x => x.Version));
        Assert.Equal("Consumed", detail.Reservation.Status);
        Assert.Equal(24.2m, detail.Order.GrossTotal);
    }

    [Fact]
    public async Task Invalid_transitions_and_cancelled_orders_do_not_add_history()
    {
        var pending = await Seed(false);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => Change(pending, Guid.NewGuid(), 0, "Processing"))).Status);
        var id = await Seed();
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => Change(id, Guid.NewGuid(), 0, "Ready"))).Status);
        Assert.Equal(400, (await Assert.ThrowsAsync<BusinessException>(() => Change(id, Guid.NewGuid(), 0, "Processing", " "))).Status);
        var op = Guid.NewGuid();
        await Change(id, op, 0, "Processing");
        await using var scope = fixture.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IOrderCancellations>().Cancel(id, "Cerere client", "admin", default);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => Change(id, Guid.NewGuid(), 1, "Ready"))).Status);
        await Change(id, op, 0, "Processing");
        var detail = (await scope.ServiceProvider.GetRequiredService<IAdminDemoOrders>().Find(id, default))!;
        Assert.Equal("Cancelling", detail.Order.Status);
        Assert.Single(detail.Processing.History);
    }

    [Fact]
    public async Task Competing_operations_for_the_same_version_allow_only_one_winner()
    {
        var id = await Seed();
        async Task<bool> Attempt()
        {
            try { await Change(id, Guid.NewGuid(), 0, "Processing"); return true; }
            catch (BusinessException e) when (e.Status == 409) { return false; }
        }
        Assert.Single(await Task.WhenAll(Attempt(), Attempt()), x => x);
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        Assert.Equal(1, await db.OrderProcessingChanges.CountAsync(x => x.OrderId == id));
    }
}
