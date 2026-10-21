using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Domain;
using SurubDirect.Ordering.Infrastructure;
using Xunit;

namespace SurubDirect.Tests;
public sealed class PreparedCartTests(OrderingCartFixture fixture) : IClassFixture<OrderingCartFixture>
{
    private async Task<(string Owner, ShoppingCart Cart, PreparationCatalog Catalog)> Setup()
    {
        var owner = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var catalog = new PreparationCatalog();
        await using var scope = fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IShoppingCartRepository>();
        var cart = await repo.Create(owner, default);
        await repo.Replace(owner, cart.Id, 1, [new("surub", catalog.First, 2), new("surub", catalog.Second, 3)], default);
        var saved = await repo.SaveCustomer(owner, cart.Id, 2, CustomerDraftSamples.Person(), default);
        return (owner, saved, catalog);
    }

    [Fact]
    public async Task Creates_owned_snapshot_and_aggregated_plan_atomically_and_replays_without_repricing()
    {
        var(owner, cart, catalog) = await Setup();
        await using var scope = fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IShoppingCartRepository>();
        var prepared = scope.ServiceProvider.GetRequiredService<IPreparedCarts>();
        var handler = new PrepareCartHandler(repo, prepared, new(catalog));
        var command = new PrepareCart(Guid.NewGuid(), cart.Id, cart.Version);
        var snapshot = await handler.Handle(owner, command, default);
        Assert.Equal(42.35m, snapshot.GrossTotal);
        Assert.Equal(35m, snapshot.NetTotal);
        Assert.Equal(2, snapshot.Lines.Count);
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var plan = await db.ReservationPlans.AsNoTracking().SingleAsync(x => x.Id == command.RequestId);
        Assert.Equal(35, Assert.Single(plan.Lines).Quantity);
        Assert.Equal(catalog.VariantId, plan.Lines[0].VariantId);
        Assert.Equal(command.RequestId, (await repo.Find(owner, default))!.PreparedId);
        catalog.Missing = true;
        Assert.Equal(snapshot.GrossTotal, (await handler.Handle(owner, command, default)).GrossTotal);
        Assert.Equal(1, catalog.Reads);
        Assert.Equal(401, (await Assert.ThrowsAsync<BusinessException>(() => prepared.Read(new string ('0', 64), default))).Status);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => repo.Replace(owner, cart.Id, cart.Version + 1, [], default))).Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Invalid_catalog_or_unavailability_never_creates_plan(bool unavailable)
    {
        var(owner, cart, catalog) = await Setup();
        catalog.Missing = !unavailable;
        catalog.Unavailable = unavailable;
        await using var scope = fixture.Services.CreateAsyncScope();
        var handler = new PrepareCartHandler(scope.ServiceProvider.GetRequiredService<IShoppingCartRepository>(), scope.ServiceProvider.GetRequiredService<IPreparedCarts>(), new(catalog));
        var id = Guid.NewGuid();
        var error = await Assert.ThrowsAsync<BusinessException>(() => handler.Handle(owner, new(id, cart.Id, cart.Version), default));
        Assert.Equal(unavailable ? 503 : 409, error.Status);
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        Assert.False(await db.ReservationPlans.AnyAsync(x => x.Id == id));
        Assert.False(await db.PreparedCarts.AnyAsync(x => x.Id == id));
    }

    [Fact]
    public async Task Stale_snapshot_is_rejected_at_commit_and_cancel_waits_for_remote_confirmation()
    {
        var(owner, cart, catalog) = await Setup();
        var id = Guid.NewGuid();
        var line = new PreparedCartLine(catalog.First, catalog.VariantId, "Demo", "SKU", "Cutie", 2, 10, 10, 12.1m, 20, 24.2m);
        var stale = PreparedCart.Create(id, cart, [line], "RON", DateTimeOffset.UtcNow);
        var plan = ReservationPlan.Create(id, id, [new(catalog.VariantId, 20)], DateTimeOffset.UtcNow);
        await using var scope = fixture.Services.CreateAsyncScope();
        var carts = scope.ServiceProvider.GetRequiredService<IShoppingCartRepository>();
        var prepared = scope.ServiceProvider.GetRequiredService<IPreparedCarts>();
        await carts.SaveCustomer(owner, cart.Id, cart.Version, CustomerDraftSamples.Person(), default);
        await Assert.ThrowsAsync<BusinessException>(() => prepared.Save(owner, stale, plan, default));
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        Assert.False(await db.ReservationPlans.AnyAsync(x => x.Id == id));
        var fresh = (await carts.Find(owner, default))!;
        var result = await new PrepareCartHandler(carts, prepared, new(catalog)).Handle(owner, new(id, fresh.Id, fresh.Version), default);
        await prepared.Cancel(owner, id, default);
        Assert.Equal(id, (await carts.Find(owner, default))!.PreparedId);
        Assert.Equal("Cancelling", (await prepared.Read(owner, default))!.Status);
        db.ChangeTracker.Clear();
        var active = await db.ReservationPlans.SingleAsync(x => x.Id == id);
        active.Observe("Released", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();
        await prepared.Cancel(owner, id, default);
        Assert.Null((await carts.Find(owner, default))!.PreparedId);
        Assert.False(await db.PreparedCarts.AnyAsync(x => x.Id == id));
        Assert.True(await db.ReservationPlans.AnyAsync(x => x.Id == id));
    }

    [Fact]
    public async Task Concurrent_preparations_create_only_one_snapshot_and_plan()
    {
        var(owner, cart, catalog) = await Setup();
        async Task<bool> Prepare()
        {
            await using var scope = fixture.Services.CreateAsyncScope();
            var handler = new PrepareCartHandler(scope.ServiceProvider.GetRequiredService<IShoppingCartRepository>(), scope.ServiceProvider.GetRequiredService<IPreparedCarts>(), new(catalog));
            try
            {
                await handler.Handle(owner, new(Guid.NewGuid(), cart.Id, cart.Version), default);
                return true;
            }
            catch (BusinessException e)
            {
                Assert.Equal(409, e.Status);
                return false;
            }
        }

        Assert.Single(await Task.WhenAll(Prepare(), Prepare()), x => x);
        await using var check = fixture.Services.CreateAsyncScope();
        var db = check.ServiceProvider.GetRequiredService<OrderingDbContext>();
        Assert.Equal(1, await db.PreparedCarts.CountAsync(x => x.CartId == cart.Id));
        var saved = await db.Carts.AsNoTracking().SingleAsync(x => x.Id == cart.Id);
        Assert.True(await db.ReservationPlans.AnyAsync(x => x.Id == saved.PreparedId));
    }
}
