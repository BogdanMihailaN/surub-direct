using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Domain;
using SurubDirect.Ordering.Infrastructure;
using Xunit;

namespace SurubDirect.Tests;
public sealed class DemoOrderCancellationTests(ReservationCoordinatorFixture fixture) : IClassFixture<ReservationCoordinatorFixture>
{
    private async Task<(string Owner, Guid Id)> Seed()
    {
        var owner = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        await using var scope = fixture.Services.CreateAsyncScope();
        var carts = scope.ServiceProvider.GetRequiredService<IShoppingCartRepository>();
        var cart = await carts.Create(owner, default);
        var catalog = new PreparationCatalog();
        cart = await carts.Replace(owner, cart.Id, cart.Version, [new("surub", catalog.First, 2), new("surub", catalog.Second, 3)], default);
        cart = await carts.SaveCustomer(owner, cart.Id, cart.Version, CustomerDraftSamples.Person(), default);
        var id = Guid.NewGuid();
        var handler = new PrepareCartHandler(carts, scope.ServiceProvider.GetRequiredService<IPreparedCarts>(), new(catalog));
        await handler.Handle(owner, new(id, cart.Id, cart.Version), default);
        await scope.ServiceProvider.GetRequiredService<ProcessReservationPlanHandler>().Handle(default);
        return (owner, id);
    }

    [Fact]
    public async Task Cancellation_intent_is_atomic_and_recovers_lost_restoration_response()
    {
        var(owner, id) = await Seed();
        await using var scope = fixture.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var orders = services.GetRequiredService<IDemoOrders>();
        var cancel = services.GetRequiredService<IOrderCancellations>();
        await orders.Confirm(owner, id, default);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => cancel.Cancel(id, "Cerere client", "admin", default))).Status);
        await services.GetRequiredService<ProcessReservationPlanHandler>().Handle(default);
        async Task<string> Request(string reason)
        {
            await using var other = fixture.Services.CreateAsyncScope();
            return (await other.ServiceProvider.GetRequiredService<IOrderCancellations>().Cancel(id, reason, "admin", default)).Status;
        }

        var results = await Task.WhenAll(Request("Cerere client"), Request("Cerere client"));
        Assert.All(results, x => Assert.Equal("Cancelling", x));
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => Request("Alt motiv"))).Status);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => orders.Complete(owner, id, default))).Status);
        var previousRestoreCalls = fixture.Remote.RestoreCalls;
        fixture.Remote.LoseRestoreResponse = true;
        await services.GetRequiredService<ProcessReservationPlanHandler>().Handle(default);
        Assert.Equal("Restored", fixture.Remote.Records[id].Status);
        Assert.Equal("Cancelling", Assert.Single((await orders.Read(owner, 1, default)).Items).Status);
        var db = services.GetRequiredService<OrderingDbContext>();
        await db.ReservationPlans.Where(x => x.Id == id).ExecuteUpdateAsync(x => x.SetProperty(v => v.NextAttemptAt, DateTimeOffset.UtcNow.AddSeconds(-1)).SetProperty(v => v.Deadline, DateTimeOffset.UtcNow.AddHours(-1)));
        await services.GetRequiredService<ProcessReservationPlanHandler>().Handle(default);
        Assert.Equal("Cancelled", await Request("Cerere client"));
        var audit = await services.GetRequiredService<IAdminDemoOrders>().Find(id, default);
        Assert.Equal("Cerere client", audit!.CancellationReason);
        Assert.Equal("admin", audit.CancelledBy);
        Assert.NotNull(audit.CancelledAt);
        Assert.Equal(42.35m, audit.Order.GrossTotal);
        Assert.Equal(previousRestoreCalls + 2, fixture.Remote.RestoreCalls);
        await orders.Complete(owner, id, default);
        Assert.Empty((await services.GetRequiredService<IShoppingCartRepository>().Find(owner, default))!.Lines);
        Assert.Equal("Cancelled", (await orders.Confirm(owner, id, default)).Status);
        Assert.Equal(0, await services.GetRequiredService<ProcessReservationPlanHandler>().Handle(default));
    }

    [Fact]
    public async Task Restoration_survives_failed_local_commit_and_keeps_original_audit()
    {
        var(owner, id) = await Seed();
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var s = scope.ServiceProvider;
            await s.GetRequiredService<IDemoOrders>().Confirm(owner, id, default);
            await s.GetRequiredService<ProcessReservationPlanHandler>().Handle(default);
            await s.GetRequiredService<IOrderCancellations>().Cancel(id, "Duplicat client", "first-admin", default);
            await Assert.ThrowsAsync<InvalidOperationException>(() => s.GetRequiredService<IReservationPlans>().Process(async (plan, ct) =>
            {
                var observation = await fixture.Remote.Restore(plan, ct);
                plan.Observe(observation.Status, observation.ExpiresAt, DateTimeOffset.UtcNow);
                throw new InvalidOperationException("Failed local commit");
            }, default));
        }

        await using var next = fixture.Services.CreateAsyncScope();
        var services = next.ServiceProvider;
        await services.GetRequiredService<ProcessReservationPlanHandler>().Handle(default);
        await services.GetRequiredService<IOrderCancellations>().Cancel(id, "Duplicat client", "second-admin", default);
        var result = await services.GetRequiredService<IAdminDemoOrders>().Find(id, default);
        Assert.Equal("Cancelled", result!.Order.Status);
        Assert.Equal("first-admin", result.CancelledBy);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    public async Task Invalid_reason_does_not_create_intent(string reason)
    {
        var(owner, id) = await Seed();
        await using var scope = fixture.Services.CreateAsyncScope();
        var s = scope.ServiceProvider;
        await s.GetRequiredService<IDemoOrders>().Confirm(owner, id, default);
        await s.GetRequiredService<ProcessReservationPlanHandler>().Handle(default);
        Assert.Equal(400, (await Assert.ThrowsAsync<BusinessException>(() => s.GetRequiredService<CancelDemoOrderHandler>().Handle(id, reason, "admin", default))).Status);
        Assert.Equal("Consumed", (await s.GetRequiredService<IReservationPlans>().Read(id, default))!.Status);
    }
}
