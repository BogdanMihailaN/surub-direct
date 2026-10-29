using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Domain;
using SurubDirect.Ordering.Infrastructure;
using Xunit;

namespace SurubDirect.Tests;
public sealed class AccountCartTests(ReservationCoordinatorFixture fixture) : IClassFixture<ReservationCoordinatorFixture>
{
    private static string NewId() => Guid.NewGuid().ToString();
    private static string NewOwner() => Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

    [Fact]
    public async Task Concurrent_creation_returns_one_cart_and_reads_are_account_scoped()
    {
        var account = NewId();
        async Task<ShoppingCart> Create()
        {
            await using var scope = fixture.Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<IAccountCarts>().Create(account, default);
        }
        var results = await Task.WhenAll(Create(), Create());
        Assert.Equal(results[0].Id, results[1].Id);
        Assert.Equal(results[0].OwnerHash, results[1].OwnerHash);
        await using var check = fixture.Services.CreateAsyncScope();
        var carts = check.ServiceProvider.GetRequiredService<IAccountCarts>();
        Assert.Null(await carts.Find(NewId(), default));
        Assert.Null(await check.ServiceProvider.GetRequiredService<IShoppingCartRepository>().Find(results[0].OwnerHash, default));
        Assert.Equal(results[0].Id, (await carts.Find(account, default))!.Id);
        var command = new ReplaceCart(results[0].Id, 1, [new("surub", Guid.NewGuid(), 2)]);
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => carts.Replace(NewId(), command, default))).Status);
        var changed = await carts.Replace(account, command, default);
        Assert.Equal(2, Assert.Single(changed.Lines).Packs);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => carts.Replace(account, command, default))).Status);
        Assert.Equal(changed.Version, (await carts.Create(account, default)).Version);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Import_moves_selection_atomically_and_only_copies_customer_when_requested(bool includeCustomer)
    {
        var account = NewId();
        var owner = NewOwner();
        await using var scope = fixture.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var guests = services.GetRequiredService<IShoppingCartRepository>();
        var carts = services.GetRequiredService<IAccountCarts>();
        var source = await guests.Create(owner, default);
        source = await guests.Replace(owner, source.Id, source.Version, [new("surub", Guid.NewGuid(), 3)], default);
        source = await guests.SaveCustomer(owner, source.Id, source.Version, CustomerDraftSamples.Person(), default);
        var target = await carts.Create(account, default);
        var command = new ImportGuestCart(source.Id, source.Version, target.Id, target.Version, includeCustomer);
        var moved = await carts.Import(account, owner, command, default);
        Assert.Equal(3, Assert.Single(moved.Lines).Packs);
        Assert.Equal(includeCustomer ? source.Customer : null, moved.Customer);
        var emptied = (await guests.Find(owner, default))!;
        Assert.Empty(emptied.Lines);
        Assert.Null(emptied.Customer);
        Assert.Equal(source.Version + 1, emptied.Version);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => carts.Import(account, owner, command, default))).Status);
        Assert.Single((await carts.Find(account, default))!.Lines);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => guests.Replace(owner, source.Id, source.Version, source.Lines, default))).Status);
    }

    [Fact]
    public async Task Import_rejects_nonempty_destination_and_stale_versions_without_changing_source()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var guests = services.GetRequiredService<IShoppingCartRepository>();
        var carts = services.GetRequiredService<IAccountCarts>();
        var account = NewId();
        var owner = NewOwner();
        var source = await guests.Create(owner, default);
        source = await guests.Replace(owner, source.Id, source.Version, [new("surub", Guid.NewGuid(), 2)], default);
        var target = await carts.Create(account, default);
        var command = new ImportGuestCart(source.Id, source.Version, target.Id, target.Version, false);
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => carts.Import(account, NewOwner(), command, default))).Status);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => carts.Import(account, owner, command with { Version = 1 }, default))).Status);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => carts.Import(account, owner, command with { TargetVersion = 99 }, default))).Status);
        target = await carts.Replace(account, new(target.Id, target.Version, [new("alt-surub", Guid.NewGuid(), 5)]), default);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => carts.Import(account, owner, command with { TargetVersion = target.Version }, default))).Status);
        Assert.Equal(2, Assert.Single((await guests.Find(owner, default))!.Lines).Packs);
        Assert.Equal(5, Assert.Single((await carts.Find(account, default))!.Lines).Packs);
    }

    [Fact]
    public async Task Concurrent_imports_into_different_accounts_have_one_winner()
    {
        var owner = NewOwner();
        ShoppingCart source;
        var first = NewId();
        var second = NewId();
        ShoppingCart a, b;
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var guests = scope.ServiceProvider.GetRequiredService<IShoppingCartRepository>();
            var carts = scope.ServiceProvider.GetRequiredService<IAccountCarts>();
            source = await guests.Create(owner, default);
            source = await guests.Replace(owner, source.Id, source.Version, [new("surub", Guid.NewGuid(), 2)], default);
            a = await carts.Create(first, default);
            b = await carts.Create(second, default);
        }
        async Task<bool> Import(string account, ShoppingCart target)
        {
            await using var scope = fixture.Services.CreateAsyncScope();
            try
            {
                await scope.ServiceProvider.GetRequiredService<IAccountCarts>().Import(account, owner, new(source.Id, source.Version, target.Id, target.Version, false), default);
                return true;
            }
            catch (BusinessException e) { Assert.Equal(409, e.Status); return false; }
        }
        var results = await Task.WhenAll(Import(first, a), Import(second, b));
        Assert.Single(results, x => x);
    }

    [Fact]
    public async Task Expired_cart_requires_explicit_restart_and_clears_customer_and_lines()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var account = NewId();
        var carts = services.GetRequiredService<IAccountCarts>();
        var cart = await carts.Create(account, default);
        cart = await carts.Replace(account, new(cart.Id, cart.Version, [new("surub", Guid.NewGuid(), 1)]), default);
        cart = await carts.SaveCustomer(account, new(cart.Id, cart.Version, CustomerDraftSamples.Person()), default);
        await services.GetRequiredService<OrderingDbContext>().Carts.Where(x => x.Id == cart.Id).ExecuteUpdateAsync(x => x.SetProperty(c => c.ExpiresAt, DateTimeOffset.UtcNow.AddDays(-1)));
        Assert.Equal(410, (await Assert.ThrowsAsync<BusinessException>(() => services.GetRequiredService<GetAccountCartHandler>().Handle(account, default))).Status);
        var restarted = await carts.Create(account, default);
        Assert.Equal(cart.Id, restarted.Id);
        Assert.True(restarted.Version > cart.Version);
        Assert.Empty(restarted.Lines);
        Assert.Null(restarted.Customer);
        Assert.True(restarted.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Prepared_guest_cart_cannot_be_transferred()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var guests = services.GetRequiredService<IShoppingCartRepository>();
        var carts = services.GetRequiredService<IAccountCarts>();
        var account = NewId();
        var owner = NewOwner();
        var source = await guests.Create(owner, default);
        source = await guests.Replace(owner, source.Id, source.Version, [new("surub", Guid.NewGuid(), 2)], default);
        source = await guests.SaveCustomer(owner, source.Id, source.Version, CustomerDraftSamples.Person(), default);
        var db = services.GetRequiredService<OrderingDbContext>();
        source.AttachPreparation(Guid.NewGuid(), source.Version, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();
        var target = await carts.Create(account, default);
        var command = new ImportGuestCart(source.Id, source.Version, target.Id, target.Version, true);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => carts.Import(account, owner, command, default))).Status);
        Assert.NotNull((await guests.Find(owner, default))!.PreparedId);
        Assert.Empty((await carts.Find(account, default))!.Lines);
    }

}
