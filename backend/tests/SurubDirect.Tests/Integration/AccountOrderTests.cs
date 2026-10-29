using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Infrastructure;
using Xunit;

namespace SurubDirect.Tests;
public sealed class AccountOrderTests(ReservationCoordinatorFixture fixture) : IClassFixture<ReservationCoordinatorFixture>
{
    private async Task<(string Owner, Guid Id)> Seed(bool complete = true)
    {
        var owner = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        await using var scope = fixture.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var carts = services.GetRequiredService<IShoppingCartRepository>();
        var cart = await carts.Create(owner, default);
        var catalog = new PreparationCatalog();
        cart = await carts.Replace(owner, cart.Id, cart.Version, [new("surub", catalog.First, 2)], default);
        cart = await carts.SaveCustomer(owner, cart.Id, cart.Version, CustomerDraftSamples.Person(), default);
        var id = Guid.NewGuid();
        await new PrepareCartHandler(carts, services.GetRequiredService<IPreparedCarts>(), new(catalog)).Handle(owner, new(id, cart.Id, cart.Version), default);
        await services.GetRequiredService<ProcessReservationPlanHandler>().Handle(default);
        await services.GetRequiredService<IDemoOrders>().Confirm(owner, id, default);
        await services.GetRequiredService<ProcessReservationPlanHandler>().Handle(default);
        if (complete) await services.GetRequiredService<IDemoOrders>().Complete(owner, id, default);
        return (owner, id);
    }

    [Fact]
    public async Task Link_is_idempotent_and_removes_guest_access()
    {
        var (owner, id) = await Seed();
        var account = Guid.NewGuid().ToString();
        await using var scope = fixture.Services.CreateAsyncScope();
        var accounts = scope.ServiceProvider.GetRequiredService<IAccountOrders>();
        var guest = scope.ServiceProvider.GetRequiredService<IDemoOrders>();
        var before = Assert.Single((await guest.Read(owner, 1, default)).Items);
        await accounts.Link(account, owner, id, default);
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var linkedAt = (await db.DemoOrders.AsNoTracking().SingleAsync(x => x.Id == id)).AccountLinkedAt;
        Assert.NotNull(linkedAt);
        await accounts.Link(account, owner, id, default);
        Assert.Equal(linkedAt, (await db.DemoOrders.AsNoTracking().SingleAsync(x => x.Id == id)).AccountLinkedAt);
        Assert.Empty((await guest.Read(owner, 1, default)).Items);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => guest.Confirm(owner, id, default))).Status);
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => guest.Complete(owner, id, default))).Status);
        var after = Assert.Single((await accounts.Read(account, 1, default)).Items);
        Assert.Equal(before.Number, after.Number);
        Assert.Equal(before.Customer, after.Customer);
        Assert.Equal(before.Lines, after.Lines);
        Assert.Empty((await accounts.Read("other", 1, default)).Items);
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => accounts.Link("other", owner, id, default))).Status);
        await db.Carts.Where(x => x.OwnerHash == owner).ExecuteUpdateAsync(x => x.SetProperty(c => c.ExpiresAt, DateTimeOffset.UtcNow.AddDays(-1)));
        Assert.Single((await accounts.Read(account, 1, default)).Items);
    }

    [Fact]
    public async Task Wrong_owner_and_unfinished_cart_are_rejected()
    {
        var (owner, id) = await Seed(false);
        var account = Guid.NewGuid().ToString();
        await using var scope = fixture.Services.CreateAsyncScope();
        var accounts = scope.ServiceProvider.GetRequiredService<IAccountOrders>();
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => accounts.Link(account, new string('0', 64), id, default))).Status);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => accounts.Link(account, owner, id, default))).Status);
        Assert.Null((await scope.ServiceProvider.GetRequiredService<OrderingDbContext>().DemoOrders.AsNoTracking().SingleAsync(x => x.Id == id)).AccountId);
        await scope.ServiceProvider.GetRequiredService<IDemoOrders>().Complete(owner, id, default);
        await accounts.Link(account, owner, id, default);
        Assert.Single((await accounts.Read(account, 1, default)).Items);
    }

    [Fact]
    public async Task Concurrent_accounts_have_exactly_one_winner()
    {
        var (owner, id) = await Seed();
        var first = Guid.NewGuid().ToString();
        var second = Guid.NewGuid().ToString();
        async Task<bool> Link(string account)
        {
            await using var scope = fixture.Services.CreateAsyncScope();
            try { await scope.ServiceProvider.GetRequiredService<IAccountOrders>().Link(account, owner, id, default); return true; }
            catch (BusinessException e) { Assert.Equal(404, e.Status); return false; }
        }
        var results = await Task.WhenAll(Link(first), Link(second));
        Assert.Single(results, x => x);
        await using var check = fixture.Services.CreateAsyncScope();
        var accounts = check.ServiceProvider.GetRequiredService<IAccountOrders>();
        Assert.Single((await accounts.Read(results[0] ? first : second, 1, default)).Items);
        Assert.Empty((await accounts.Read(results[0] ? second : first, 1, default)).Items);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100001)]
    public async Task Invalid_page_is_rejected(int page)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        Assert.Equal(400, (await Assert.ThrowsAsync<BusinessException>(() => scope.ServiceProvider.GetRequiredService<GetAccountOrdersHandler>().Handle("account", page, default))).Status);
    }
}
