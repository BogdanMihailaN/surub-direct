using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Infrastructure;
using Xunit;
namespace SurubDirect.Tests;
public sealed class OrderReturnPersistenceTests(OrderingCartFixture fixture) : IClassFixture<OrderingCartFixture>
{
    private async Task<Guid> Seed()
    {
        var (order, plan) = ReturnSamples.Create();
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        db.Add(order); db.Add(plan); await db.SaveChangesAsync(); return order.Id;
    }
    private async Task Change(Guid id, Guid operation, long version, string target, bool received = false, string actor = "admin")
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ChangeReturnHandler>().Handle(id, operation, version, target, "Test return", actor, received, default);
    }
    [Fact]
    public async Task Receipt_is_atomic_idempotent_and_waits_for_inventory_without_refunding()
    {
        var id = await Seed(); var request = Guid.NewGuid();
        await Task.WhenAll(Change(id, request, 0, "Requested"), Change(id, request, 0, "Requested"));
        await Change(id, request, 0, "Requested", actor: "other-admin");
        await Change(id, Guid.NewGuid(), 1, "Approved");
        await Assert.ThrowsAsync<BusinessException>(() => Change(id, Guid.NewGuid(), 2, "Restoring"));
        await using var scope = fixture.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var before = (await services.GetRequiredService<IAdminDemoOrders>().Find(id, default))!;
        Assert.Equal("Consumed", before.Reservation.Status);
        Assert.Equal("Approved", before.Return.Status);
        var receipt = Guid.NewGuid();
        await Task.WhenAll(Change(id, receipt, 2, "Restoring", true), Change(id, receipt, 2, "Restoring", true));
        var waiting = (await services.GetRequiredService<IAdminDemoOrders>().Find(id, default))!;
        Assert.Equal("Restoring", waiting.Return.Status);
        Assert.Equal("Restoring", waiting.Reservation.Status);
        Assert.Null(waiting.Return.RestoredAt);
        Assert.Equal(3, waiting.Return.History.Count);
        Assert.Equal("admin", waiting.Return.History[0].Actor);
        var plans = services.GetRequiredService<IReservationPlans>();
        await plans.Process((plan, _) => { plan.ObserveOutcome("Restored", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow); return Task.CompletedTask; }, default);
        await Change(id, receipt, 2, "Restoring", true);
        var complete = (await services.GetRequiredService<IAdminDemoOrders>().Find(id, default))!;
        Assert.Equal("Restored", complete.Return.Status);
        Assert.NotNull(complete.Return.RestoredAt);
        Assert.Equal("NotStarted", complete.PaymentSimulation.Status);
        Assert.Equal("Confirmed", complete.Order.Status);
        Assert.Equal(3, complete.Return.History.Count);
        Assert.Equal(0, await plans.Process((_, _) => throw new Exception("Terminal plan must not run"), default));
        var publicOrder = Assert.Single((await services.GetRequiredService<IDemoOrders>().Read(new string('a', 64), 1, default)).Items, x => x.Id == id);
        Assert.Equal("Restored", publicOrder.Return.Status);
    }
    [Fact]
    public async Task Concurrent_approval_and_rejection_have_one_winner()
    {
        var id = await Seed(); await Change(id, Guid.NewGuid(), 0, "Requested");
        async Task<bool> Attempt(string target)
        {
            try { await Change(id, Guid.NewGuid(), 1, target); return true; }
            catch (BusinessException e) when (e.Status == 409) { return false; }
        }
        Assert.Single(await Task.WhenAll(Attempt("Approved"), Attempt("Rejected")), x => x);
        await using var scope = fixture.Services.CreateAsyncScope();
        var detail = (await scope.ServiceProvider.GetRequiredService<IAdminDemoOrders>().Find(id, default))!;
        Assert.Equal(2, detail.Return.History.Count);
        Assert.Equal("Consumed", detail.Reservation.Status);
    }
    [Fact]
    public async Task Rejected_return_is_terminal_and_does_not_restore_stock()
    {
        var id = await Seed(); await Change(id, Guid.NewGuid(), 0, "Requested");
        await Change(id, Guid.NewGuid(), 1, "Rejected");
        await Assert.ThrowsAsync<BusinessException>(() => Change(id, Guid.NewGuid(), 2, "Restoring", true));
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        Assert.False((await db.ReservationPlans.AsNoTracking().SingleAsync(x => x.Id == id)).RestorationRequested);
        Assert.Equal(2, await db.ReturnChanges.CountAsync(x => x.OrderId == id));
    }
}
