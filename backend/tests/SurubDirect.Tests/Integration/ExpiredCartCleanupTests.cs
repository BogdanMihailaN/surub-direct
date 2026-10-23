using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Domain;
using SurubDirect.Ordering.Infrastructure;
using Xunit;

namespace SurubDirect.Tests;
public sealed class ExpiredCartCleanupTests(OrderingCartFixture fixture) : IClassFixture<OrderingCartFixture>
{
    private async Task<ShoppingCart[]> Seed()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        await db.DemoOrders.ExecuteDeleteAsync();
        await db.PreparedCarts.ExecuteDeleteAsync();
        await db.ReservationPlans.ExecuteDeleteAsync();
        await db.Carts.ExecuteDeleteAsync();
        var now = DateTimeOffset.UtcNow;
        ShoppingCart Cart(int ageDays)
        {
            var created = now.AddDays(-ageDays);
            var cart = ShoppingCart.Create(Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)), created);
            cart.Replace(cart.Id, cart.Version, [new("surub", Guid.NewGuid(), 2)], created);
            cart.SaveCustomer(cart.Id, cart.Version, CustomerDraftSamples.Person(), created);
            return cart;
        }

        var rows = new[]
        {
            Cart(60),
            Cart(59),
            Cart(32),
            Cart(0),
            Cart(60),
            Cart(60),
            Cart(60)
        };
        PreparedCart Snapshot(ShoppingCart cart) => PreparedCart.Create(Guid.NewGuid(), cart, [new(cart.Lines[0].OfferId, Guid.NewGuid(), "Demo", "SKU", "Bucată", 2, 1, 1, 1, 2, 2)], "RON", now.AddDays(-60));
        var attached = Snapshot(rows[4]);
        rows[4].AttachPreparation(attached.Id, rows[4].Version, now.AddDays(-60));
        db.PreparedCarts.Add(attached);
        db.ReservationPlans.Add(ReservationPlan.Create(attached.Id, attached.Id, [new(attached.Lines[0].VariantId, 2)], now.AddDays(-60)));
        // Even detached/inconsistent historical references protect a cart.
        db.PreparedCarts.Add(Snapshot(rows[5]));
        db.DemoOrders.Add(DemoOrder.Create(rows[6].OwnerHash, Snapshot(rows[6]), now.AddDays(-60)));
        db.Carts.AddRange(rows);
        await db.SaveChangesAsync();
        return rows;
    }

    private async Task<SurubDirect.Ordering.Contracts.ExpiredCartCleanupResult> Run(bool dryRun, int batch = 100)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IExpiredCartCleanup>().Run(7, batch, dryRun, default);
    }

    [Fact]
    public async Task Simulation_does_not_delete_customer_drafts_or_selections()
    {
        await Seed();
        var result = await Run(true, 1);
        Assert.Equal(1, result.Candidates);
        Assert.Equal(0, result.Removed);
        Assert.True(result.DryRun);
        await using var scope = fixture.Services.CreateAsyncScope();
        var carts = await scope.ServiceProvider.GetRequiredService<OrderingDbContext>().Carts.ToArrayAsync();
        Assert.Equal(7, carts.Length);
        Assert.All(carts, cart =>
        {
            Assert.NotNull(cart.Customer);
            Assert.Single(cart.Lines);
        });
    }

    [Fact]
    public async Task Cleanup_excludes_active_grace_period_preparations_and_orders()
    {
        var before = await Seed();
        Assert.Equal(2, (await Run(false)).Removed);
        Assert.Equal(0, (await Run(false)).Removed);
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var carts = await db.Carts.ToArrayAsync();
        Assert.Equal(before.Skip(2).Select(x => x.Id).Order(), carts.Select(x => x.Id).Order());
        Assert.Equal(2, await db.PreparedCarts.CountAsync());
        Assert.Equal(1, await db.DemoOrders.CountAsync());
        Assert.Equal(1, await db.ReservationPlans.CountAsync());
    }

    [Fact]
    public async Task Deleted_cart_cannot_be_mutated_and_owner_can_start_a_new_cart()
    {
        var before = await Seed();
        await Run(false);
        await using var scope = fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IShoppingCartRepository>();
        Assert.Null(await scope.ServiceProvider.GetRequiredService<GetCartHandler>().Handle(before[0].OwnerHash, default));
        var error = await Assert.ThrowsAsync<BusinessException>(() => repo.Replace(before[0].OwnerHash, before[0].Id, before[0].Version, [], default));
        Assert.Equal(401, error.Status);
        var fresh = await repo.Create(Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)), default);
        Assert.NotEqual(before[0].Id, fresh.Id);
        Assert.Null(fresh.Customer);
        Assert.Empty(fresh.Lines);
    }

    [Fact]
    public async Task Concurrent_batches_have_no_double_deletion()
    {
        await Seed();
        var results = await Task.WhenAll(Run(false, 1), Run(false, 1));
        Assert.Equal(2, results.Sum(x => x.Removed));
        Assert.Equal(0, (await Run(false)).Candidates);
    }

    [Fact]
    public async Task Locked_cart_is_skipped_and_rechecked_after_transaction_commits()
    {
        var before = await Seed();
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            var expiry = DateTimeOffset.UtcNow.AddDays(1);
            await db.Database.ExecuteSqlInterpolatedAsync($"""UPDATE ordering."Carts" SET "ExpiresAt"={expiry} WHERE "Id"={before[0].Id}""");
            Assert.Equal(1, (await Run(false)).Removed);
            await tx.CommitAsync();
        }

        Assert.Equal(0, (await Run(false)).Removed);
        Assert.True(await db.Carts.AnyAsync(x => x.Id == before[0].Id));
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(3651, 100)]
    [InlineData(7, 0)]
    [InlineData(7, 1001)]
    public async Task Invalid_cleanup_parameters_are_rejected(int days, int batch)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => scope.ServiceProvider.GetRequiredService<IExpiredCartCleanup>().Run(days, batch, false, default));
    }
}
