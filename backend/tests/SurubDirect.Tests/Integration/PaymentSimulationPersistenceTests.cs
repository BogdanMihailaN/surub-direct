using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Infrastructure;
using Xunit;
namespace SurubDirect.Tests;
public sealed class PaymentSimulationPersistenceTests(OrderingCartFixture fixture) : IClassFixture<OrderingCartFixture>
{
    private async Task<Guid> Seed(bool confirmed = true)
    {
        var (order, plan) = PaymentSimulationSamples.Create(confirmed);
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        db.DemoOrders.Add(order); db.ReservationPlans.Add(plan);
        await db.SaveChangesAsync();
        return order.Id;
    }
    private async Task Apply(Guid id, Guid operation, long version, string action, string actor = "admin")
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SimulatePaymentHandler>().Handle(id, operation, version, action, "Test simulator", actor, default);
    }
    private async Task Cancel(Guid id)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IOrderCancellations>().Cancel(id, "Test anulare", "admin", default);
    }

    [Fact]
    public async Task Retries_keep_original_audit_and_cancellation_requires_a_resolved_or_refunded_payment()
    {
        var id = await Seed(); var start = Guid.NewGuid();
        await Task.WhenAll(Apply(id, start, 0, "Start"), Apply(id, start, 0, "Start"));
        await Apply(id, start, 0, "Start", "other-admin");
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => Apply(id, start, 0, "Succeed"))).Status);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => Cancel(id))).Status);
        await Apply(id, Guid.NewGuid(), 1, "Fail");
        await Apply(id, Guid.NewGuid(), 2, "Start");
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => Apply(id, Guid.NewGuid(), 1, "Succeed"))).Status);
        await Apply(id, Guid.NewGuid(), 3, "Succeed");
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => Cancel(id))).Status);
        var refund = Guid.NewGuid();
        await Task.WhenAll(Apply(id, refund, 4, "Refund"), Apply(id, refund, 4, "Refund"));
        await Cancel(id);
        await Apply(id, refund, 4, "Refund");
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => Apply(id, Guid.NewGuid(), 5, "Start"))).Status);
        await using var scope = fixture.Services.CreateAsyncScope();
        var detail = (await scope.ServiceProvider.GetRequiredService<IAdminDemoOrders>().Find(id, default))!;
        Assert.Equal("Cancelling", detail.Order.Status);
        Assert.Equal("Restoring", detail.Reservation.Status);
        Assert.Equal("Refunded", detail.PaymentSimulation.Status);
        Assert.Equal(44.2m, detail.PaymentSimulation.Amount);
        Assert.Equal(5, detail.PaymentSimulation.History.Count);
        Assert.Equal("admin", detail.PaymentSimulation.History[0].Actor);
        Assert.NotEqual(detail.PaymentSimulation.History[0].AttemptId, detail.PaymentSimulation.History[2].AttemptId);
    }

    [Fact]
    public async Task Conflicting_results_for_the_same_version_have_one_winner()
    {
        var id = await Seed();
        await Apply(id, Guid.NewGuid(), 0, "Start");
        async Task<bool> Result(string action)
        {
            try { await Apply(id, Guid.NewGuid(), 1, action); return true; }
            catch (BusinessException e) when (e.Status == 409) { return false; }
        }
        Assert.Single(await Task.WhenAll(Result("Succeed"), Result("Fail")), x => x);
        await using var scope = fixture.Services.CreateAsyncScope();
        var detail = (await scope.ServiceProvider.GetRequiredService<IAdminDemoOrders>().Find(id, default))!;
        Assert.Equal(2, detail.PaymentSimulation.History.Count);
        Assert.Contains(detail.PaymentSimulation.Status, new[] { "Paid", "Failed" });
        Assert.Equal("Consumed", detail.Reservation.Status);
    }

    [Fact]
    public async Task Cancellation_and_payment_start_are_serialized()
    {
        var id = await Seed();
        async Task<bool> Attempt(Func<Task> operation)
        {
            try { await operation(); return true; }
            catch (BusinessException e) when (e.Status == 409) { return false; }
        }
        Assert.Single(await Task.WhenAll(Attempt(() => Apply(id, Guid.NewGuid(), 0, "Start")), Attempt(() => Cancel(id))), x => x);
        await using var scope = fixture.Services.CreateAsyncScope();
        var detail = (await scope.ServiceProvider.GetRequiredService<IAdminDemoOrders>().Find(id, default))!;
        Assert.True((detail.Order.Status == "Confirmed" && detail.PaymentSimulation.Status == "Pending")
            || (detail.Order.Status == "Cancelling" && detail.PaymentSimulation.Status == "NotStarted"));
    }

    [Fact]
    public async Task Invalid_order_does_not_persist_a_partial_payment_or_event()
    {
        var id = await Seed(false);
        await Assert.ThrowsAsync<BusinessException>(() => Apply(id, Guid.NewGuid(), 0, "Start"));
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        Assert.False(await db.PaymentSimulations.AnyAsync(x => x.OrderId == id));
        Assert.False(await db.PaymentSimulationEvents.AnyAsync(x => x.OrderId == id));
    }
}
