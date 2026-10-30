using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Domain;
using SurubDirect.Ordering.Infrastructure;
using Xunit;

namespace SurubDirect.Tests;
public sealed class AccountPreparationTests(ReservationCoordinatorFixture fixture) : IClassFixture<ReservationCoordinatorFixture>
{
    private async Task<(string Account, ShoppingCart Cart, Guid Preparation)> Seed()
    {
        var account = Guid.NewGuid().ToString();
        await using var scope = fixture.Services.CreateAsyncScope();
        var s = scope.ServiceProvider;
        var carts = s.GetRequiredService<IAccountCarts>();
        var cart = await carts.Create(account, default);
        var catalog = new PreparationCatalog();
        cart = await carts.Replace(account, new(cart.Id, cart.Version, [new("surub", catalog.First, 2), new("surub", catalog.Second, 3)]), default);
        cart = await carts.SaveCustomer(account, new(cart.Id, cart.Version, CustomerDraftSamples.Person()), default);
        var id = Guid.NewGuid();
        var handler = new PrepareAccountCartHandler(carts, s.GetRequiredService<IPreparedCarts>(), new(catalog));
        var command = new PrepareCart(id, cart.Id, cart.Version);
        var snapshot = await handler.Handle(account, command, default);
        Assert.Equal(id, (await handler.Handle(account, command, default)).Id);
        Assert.Equal(42.35m, snapshot.GrossTotal);
        await s.GetRequiredService<ProcessReservationPlanHandler>().Handle(default);
        return (account, cart, id);
    }

    [Fact]
    public async Task Confirmation_is_idempotent_and_order_belongs_to_account_from_creation()
    {
        var (account, cart, id) = await Seed();
        async Task<string> Confirm()
        {
            await using var scope = fixture.Services.CreateAsyncScope();
            return (await scope.ServiceProvider.GetRequiredService<ConfirmAccountOrderHandler>().Handle(account, id, default)).Number;
        }
        var numbers = await Task.WhenAll(Confirm(), Confirm());
        Assert.Equal(numbers[0], numbers[1]);
        await using var check = fixture.Services.CreateAsyncScope();
        var s = check.ServiceProvider;
        var history = s.GetRequiredService<IAccountOrders>();
        Assert.Equal("Confirming", Assert.Single((await history.Read(account, 1, default)).Items).Status);
        Assert.Empty((await s.GetRequiredService<IDemoOrders>().Read(cart.OwnerHash, 1, default)).Items);
        Assert.Empty((await history.Read("other", 1, default)).Items);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => s.GetRequiredService<CompleteAccountOrderHandler>().Handle(account, id, default))).Status);
        await s.GetRequiredService<ProcessReservationPlanHandler>().Handle(default);
        var order = Assert.Single((await history.Read(account, 1, default)).Items);
        Assert.Equal("Confirmed", order.Status);
        var persisted = await s.GetRequiredService<OrderingDbContext>().DemoOrders.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal(account, persisted.AccountId);
        Assert.NotNull(persisted.AccountLinkedAt);
        await s.GetRequiredService<CompleteAccountOrderHandler>().Handle(account, id, default);
        await s.GetRequiredService<CompleteAccountOrderHandler>().Handle(account, id, default);
        var cleared = (await s.GetRequiredService<IAccountCarts>().Find(account, default))!;
        Assert.Empty(cleared.Lines);
        Assert.Null(cleared.Customer);
        Assert.Null(cleared.PreparedId);
        Assert.Equal(order.Number, await Confirm());
        Assert.Single((await history.Read(account, 1, default)).Items);
    }

    [Fact]
    public async Task Anonymous_and_other_accounts_cannot_access_preparation_even_with_internal_owner_hash()
    {
        var (account, cart, id) = await Seed();
        await using var scope = fixture.Services.CreateAsyncScope();
        var s = scope.ServiceProvider;
        var prepared = s.GetRequiredService<IPreparedCarts>();
        var orders = s.GetRequiredService<IDemoOrders>();
        Assert.Equal(id, (await s.GetRequiredService<GetAccountPreparationHandler>().Handle(account, default))!.Id);
        var db = s.GetRequiredService<OrderingDbContext>();
        var snapshot = await db.PreparedCarts.AsNoTracking().SingleAsync(x => x.Id == id);
        var plan = await db.ReservationPlans.AsNoTracking().SingleAsync(x => x.Id == id);
        foreach (var wrongAccount in new string?[] { null, "other" })
        {
            Assert.Equal(401, (await Assert.ThrowsAsync<BusinessException>(() => prepared.Save(cart.OwnerHash, snapshot, plan, default, wrongAccount))).Status);
            Assert.Equal(401, (await Assert.ThrowsAsync<BusinessException>(() => prepared.Read(cart.OwnerHash, default, wrongAccount))).Status);
            Assert.Equal(401, (await Assert.ThrowsAsync<BusinessException>(() => prepared.Cancel(cart.OwnerHash, id, default, wrongAccount))).Status);
            Assert.Equal(401, (await Assert.ThrowsAsync<BusinessException>(() => orders.Confirm(cart.OwnerHash, id, default, wrongAccount))).Status);
            Assert.Equal(401, (await Assert.ThrowsAsync<BusinessException>(() => orders.Complete(cart.OwnerHash, id, default, wrongAccount))).Status);
        }
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => s.GetRequiredService<GetAccountPreparationHandler>().Handle("other", default))).Status);
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => s.GetRequiredService<ConfirmAccountOrderHandler>().Handle("other", id, default))).Status);
        await s.GetRequiredService<CancelAccountPreparationHandler>().Handle(account, id, default);
        await s.GetRequiredService<ProcessReservationPlanHandler>().Handle(default);
        await s.GetRequiredService<CancelAccountPreparationHandler>().Handle(account, id, default);
    }

    [Fact]
    public async Task Cancellation_releases_reservation_before_restoring_editable_cart()
    {
        var (account, _, id) = await Seed();
        await using var scope = fixture.Services.CreateAsyncScope();
        var s = scope.ServiceProvider;
        var cancel = s.GetRequiredService<CancelAccountPreparationHandler>();
        await cancel.Handle(account, id, default);
        Assert.NotNull((await s.GetRequiredService<IAccountCarts>().Find(account, default))!.PreparedId);
        await s.GetRequiredService<ProcessReservationPlanHandler>().Handle(default);
        Assert.Equal("Released", fixture.Remote.Records[id].Status);
        await cancel.Handle(account, id, default);
        await cancel.Handle(account, id, default);
        var restored = (await s.GetRequiredService<IAccountCarts>().Find(account, default))!;
        Assert.Null(restored.PreparedId);
        Assert.Equal(2, restored.Lines.Count);
        Assert.NotNull(restored.Customer);
        Assert.Empty((await s.GetRequiredService<IAccountOrders>().Read(account, 1, default)).Items);
    }

    [Fact]
    public async Task Failed_confirmation_retains_selection_and_stays_in_account_history()
    {
        var (account, _, id) = await Seed();
        await using var scope = fixture.Services.CreateAsyncScope();
        var s = scope.ServiceProvider;
        await s.GetRequiredService<ConfirmAccountOrderHandler>().Handle(account, id, default);
        fixture.Remote.Records[id] = new("Expired", DateTimeOffset.UtcNow);
        await s.GetRequiredService<ProcessReservationPlanHandler>().Handle(default);
        Assert.Equal("Failed", Assert.Single((await s.GetRequiredService<IAccountOrders>().Read(account, 1, default)).Items).Status);
        await s.GetRequiredService<CompleteAccountOrderHandler>().Handle(account, id, default);
        var cart = (await s.GetRequiredService<IAccountCarts>().Find(account, default))!;
        Assert.Equal(2, cart.Lines.Count);
        Assert.NotNull(cart.Customer);
        Assert.Null(cart.PreparedId);
    }
}
