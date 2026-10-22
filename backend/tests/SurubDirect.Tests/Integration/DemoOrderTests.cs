using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Domain;
using SurubDirect.Ordering.Infrastructure;
using Xunit;

namespace SurubDirect.Tests;
public sealed class DemoOrderTests(ReservationCoordinatorFixture fixture) : IClassFixture<ReservationCoordinatorFixture>
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
    public async Task Confirmation_and_snapshot_are_atomic_idempotent_and_recover_lost_consume_response_after_deadline()
    {
        var(owner, id) = await Seed();
        async Task<string> Confirm()
        {
            await using var scope = fixture.Services.CreateAsyncScope();
            return (await scope.ServiceProvider.GetRequiredService<IDemoOrders>().Confirm(owner, id, default)).Number;
        }

        var numbers = await Task.WhenAll(Confirm(), Confirm());
        Assert.Equal(numbers[0], numbers[1]);
        Assert.StartsWith("DEMO-", numbers[0]);
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var orders = scope.ServiceProvider.GetRequiredService<IDemoOrders>();
            var order = Assert.Single((await orders.Read(owner, 1, default)).Items);
            Assert.Equal("Confirming", order.Status);
            Assert.Equal(42.35m, order.GrossTotal);
            Assert.Equal("Confirming", (await scope.ServiceProvider.GetRequiredService<IReservationPlans>().Read(id, default))!.Status);
            Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => scope.ServiceProvider.GetRequiredService<IPreparedCarts>().Cancel(owner, id, default))).Status);
            Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => orders.Complete(owner, id, default))).Status);
            fixture.Remote.LoseConsumeResponse = true;
            await scope.ServiceProvider.GetRequiredService<ProcessReservationPlanHandler>().Handle(default);
            Assert.Equal("Consumed", fixture.Remote.Records[id].Status);
            Assert.Equal("Confirming", Assert.Single((await orders.Read(owner, 1, default)).Items).Status);
        }

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
            await db.ReservationPlans.Where(x => x.Id == id).ExecuteUpdateAsync(x => x.SetProperty(p => p.Deadline, DateTimeOffset.UtcNow.AddHours(-1)).SetProperty(p => p.NextAttemptAt, DateTimeOffset.UtcNow.AddSeconds(-1)));
            await scope.ServiceProvider.GetRequiredService<ProcessReservationPlanHandler>().Handle(default);
            var orders = scope.ServiceProvider.GetRequiredService<IDemoOrders>();
            var order = Assert.Single((await orders.Read(owner, 1, default)).Items);
            Assert.Equal("Confirmed", order.Status);
            Assert.NotNull(order.FinalizedAt);
            await orders.Complete(owner, id, default);
            await orders.Complete(owner, id, default);
            var cart = (await scope.ServiceProvider.GetRequiredService<IShoppingCartRepository>().Find(owner, default))!;
            Assert.Empty(cart.Lines);
            Assert.Null(cart.Customer);
            Assert.Null(cart.PreparedId);
            Assert.False(await db.PreparedCarts.AnyAsync(x => x.Id == id));
            var retained = Assert.Single((await orders.Read(owner, 1, default)).Items);
            Assert.Equal(order.GrossTotal, retained.GrossTotal);
            Assert.Equal(order.Customer, retained.Customer);
            Assert.Equal(order.Lines, retained.Lines);
            Assert.Equal(order.Number, (await orders.Confirm(owner, id, default)).Number);
            Assert.Empty((await orders.Read(new string ('0', 64), 1, default)).Items);
            Assert.Equal(401, (await Assert.ThrowsAsync<BusinessException>(() => orders.Confirm(new string ('0', 64), id, default))).Status);
        }
    }

    [Fact]
    public async Task Expired_remote_reservation_fails_order_and_restores_editable_selection_without_consumption()
    {
        var(owner, id) = await Seed();
        await using var scope = fixture.Services.CreateAsyncScope();
        var orders = scope.ServiceProvider.GetRequiredService<IDemoOrders>();
        await orders.Confirm(owner, id, default);
        fixture.Remote.Records[id] = new("Expired", DateTimeOffset.UtcNow);
        await scope.ServiceProvider.GetRequiredService<ProcessReservationPlanHandler>().Handle(default);
        Assert.Equal("Failed", Assert.Single((await orders.Read(owner, 1, default)).Items).Status);
        await orders.Complete(owner, id, default);
        var cart = (await scope.ServiceProvider.GetRequiredService<IShoppingCartRepository>().Find(owner, default))!;
        Assert.Equal(2, cart.Lines.Count);
        Assert.NotNull(cart.Customer);
        Assert.Null(cart.PreparedId);
        Assert.Equal("Failed", (await orders.Confirm(owner, id, default)).Status);
    }

    [Fact]
    public async Task Successful_remote_consumption_survives_rolled_back_local_transaction()
    {
        var(owner, id) = await Seed();
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<IDemoOrders>().Confirm(owner, id, default);
            await Assert.ThrowsAsync<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<IReservationPlans>().Process(async (plan, ct) =>
            {
                var observed = await fixture.Remote.Consume(plan, ct);
                plan.Observe(observed.Status, observed.ExpiresAt, DateTimeOffset.UtcNow);
                throw new InvalidOperationException("Simulated failed local commit");
            }, default));
        }

        await using var next = fixture.Services.CreateAsyncScope();
        var orders = next.ServiceProvider.GetRequiredService<IDemoOrders>();
        Assert.Equal("Confirming", Assert.Single((await orders.Read(owner, 1, default)).Items).Status);
        await next.ServiceProvider.GetRequiredService<ProcessReservationPlanHandler>().Handle(default);
        Assert.Equal("Confirmed", Assert.Single((await orders.Read(owner, 1, default)).Items).Status);
        Assert.Equal("Consumed", fixture.Remote.Records[id].Status);
    }

    [Fact]
    public async Task Concurrent_cancel_and_confirm_never_leave_order_with_cancelled_intent()
    {
        var(owner, id) = await Seed();
        async Task<bool> Attempt(bool confirm)
        {
            await using var scope = fixture.Services.CreateAsyncScope();
            try
            {
                if (confirm)
                    await scope.ServiceProvider.GetRequiredService<IDemoOrders>().Confirm(owner, id, default);
                else
                    await scope.ServiceProvider.GetRequiredService<IPreparedCarts>().Cancel(owner, id, default);
                return true;
            }
            catch (BusinessException ex)
            {
                Assert.Equal(409, ex.Status);
                return false;
            }
        }

        var results = await Task.WhenAll(Attempt(true), Attempt(false));
        Assert.Single(results, x => x);
        await using var check = fixture.Services.CreateAsyncScope();
        var orders = await check.ServiceProvider.GetRequiredService<IDemoOrders>().Read(owner, 1, default);
        var plan = (await check.ServiceProvider.GetRequiredService<IReservationPlans>().Read(id, default))!;
        Assert.Equal(results[0], plan.ConfirmationRequested);
        Assert.Equal(results[1], plan.CancellationRequested);
        Assert.Equal(results[0] ? 1 : 0, orders.Total);
        await check.ServiceProvider.GetRequiredService<ProcessReservationPlanHandler>().Handle(default);
    }
}
