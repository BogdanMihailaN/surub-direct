using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Domain;
using SurubDirect.Ordering.Infrastructure;
using Xunit;

namespace SurubDirect.Tests;
public sealed class ReservationCoordinatorTests(ReservationCoordinatorFixture fixture) : IClassFixture<ReservationCoordinatorFixture>
{
    [Fact]
    public async Task Lost_response_is_reconciled_after_new_scope_then_cancelled_without_duplicate_remote_record()
    {
        var plan = ReservationPlan.Create(Guid.NewGuid(), Guid.NewGuid(), [new(Guid.NewGuid(), 3)], DateTimeOffset.UtcNow);
        fixture.Remote.LoseReserveResponse = true;
        await using (var s = fixture.Services.CreateAsyncScope())
        {
            var repo = s.ServiceProvider.GetRequiredService<IReservationPlans>();
            await repo.Save(plan, default);
            await s.ServiceProvider.GetRequiredService<ProcessReservationPlanHandler>().Handle(default);
            var pending = await repo.Read(plan.Id, default);
            Assert.Equal("Pending", pending!.Status);
            Assert.Equal(1, pending.Attempts);
        }

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        await db.ReservationPlans.Where(x => x.Id == plan.Id).ExecuteUpdateAsync(x => x.SetProperty(p => p.NextAttemptAt, DateTimeOffset.UtcNow.AddSeconds(-1)));
        var plans = scope.ServiceProvider.GetRequiredService<IReservationPlans>();
        await scope.ServiceProvider.GetRequiredService<ProcessReservationPlanHandler>().Handle(default);
        Assert.Equal("Active", (await plans.Read(plan.Id, default))!.Status);
        Assert.True(fixture.Remote.Records.ContainsKey(plan.Id));
        await plans.Cancel(plan.Id, plan.OrderId, default);
        await scope.ServiceProvider.GetRequiredService<ProcessReservationPlanHandler>().Handle(default);
        Assert.Equal("Released", (await plans.Read(plan.Id, default))!.Status);
        Assert.Equal("Released", fixture.Remote.Records[plan.Id].Status);
        var duplicate = await plans.Save(ReservationPlan.Create(plan.Id, plan.OrderId, plan.Lines, DateTimeOffset.UtcNow), default);
        Assert.Equal("Released", duplicate.Status);
        await Assert.ThrowsAsync<BusinessException>(() => plans.Save(ReservationPlan.Create(plan.Id, plan.OrderId, [new(Guid.NewGuid(), 1)], DateTimeOffset.UtcNow), default));
    }

    [Fact]
    public async Task Cancellation_before_dispatch_never_sends_reserve()
    {
        var calls = fixture.Remote.ReserveCalls;
        await using var scope = fixture.Services.CreateAsyncScope();
        var plans = scope.ServiceProvider.GetRequiredService<IReservationPlans>();
        var plan = ReservationPlan.Create(Guid.NewGuid(), Guid.NewGuid(), [new(Guid.NewGuid(), 1)], DateTimeOffset.UtcNow);
        await plans.Save(plan, default);
        await plans.Cancel(plan.Id, plan.OrderId, default);
        await scope.ServiceProvider.GetRequiredService<ProcessReservationPlanHandler>().Handle(default);
        Assert.Equal(calls, fixture.Remote.ReserveCalls);
        Assert.Equal("Released", (await plans.Read(plan.Id, default))!.Status);
    }

    [Fact]
    public async Task Rejected_creation_is_followed_by_persistent_cancellation()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var plans = scope.ServiceProvider.GetRequiredService<IReservationPlans>();
        var plan = ReservationPlan.Create(Guid.NewGuid(), Guid.NewGuid(), [new(Guid.NewGuid(), 1)], DateTimeOffset.UtcNow);
        await plans.Save(plan, default);
        fixture.Remote.Reject = true;
        try
        {
            await scope.ServiceProvider.GetRequiredService<ProcessReservationPlanHandler>().Handle(default);
        }
        finally
        {
            fixture.Remote.Reject = false;
        }

        Assert.Equal("Cancelling", (await plans.Read(plan.Id, default))!.Status);
        await scope.ServiceProvider.GetRequiredService<ProcessReservationPlanHandler>().Handle(default);
        Assert.Equal("Released", (await plans.Read(plan.Id, default))!.Status);
    }

    [Fact]
    public async Task Overdue_plan_is_cancelled_without_creating_a_late_reservation()
    {
        var calls = fixture.Remote.ReserveCalls;
        await using var scope = fixture.Services.CreateAsyncScope();
        var plans = scope.ServiceProvider.GetRequiredService<IReservationPlans>();
        var plan = ReservationPlan.Create(Guid.NewGuid(), Guid.NewGuid(), [new(Guid.NewGuid(), 1)], DateTimeOffset.UtcNow.AddHours(-1));
        await plans.Save(plan, default);
        await scope.ServiceProvider.GetRequiredService<ProcessReservationPlanHandler>().Handle(default);
        Assert.Equal(calls, fixture.Remote.ReserveCalls);
        Assert.Equal("Released", (await plans.Read(plan.Id, default))!.Status);
    }

    [Fact]
    public async Task Early_cancel_and_lost_release_response_cannot_be_undone_by_late_create()
    {
        var id = Guid.NewGuid();
        var order = Guid.NewGuid();
        await using var scope = fixture.Services.CreateAsyncScope();
        var plans = scope.ServiceProvider.GetRequiredService<IReservationPlans>();
        await plans.Cancel(id, order, default);
        fixture.Remote.LoseReleaseResponse = true;
        await scope.ServiceProvider.GetRequiredService<ProcessReservationPlanHandler>().Handle(default);
        Assert.Equal("Cancelling", (await plans.Read(id, default))!.Status);
        var late = await plans.Save(ReservationPlan.Create(id, order, [new(Guid.NewGuid(), 1)], DateTimeOffset.UtcNow), default);
        Assert.True(late.CancellationRequested);
        await scope.ServiceProvider.GetRequiredService<OrderingDbContext>().ReservationPlans.Where(x => x.Id == id).ExecuteUpdateAsync(x => x.SetProperty(p => p.NextAttemptAt, DateTimeOffset.UtcNow.AddSeconds(-1)));
        await scope.ServiceProvider.GetRequiredService<ProcessReservationPlanHandler>().Handle(default);
        Assert.Equal("Released", (await plans.Read(id, default))!.Status);
    }
}
