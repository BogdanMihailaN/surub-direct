using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Domain;
using Xunit;

namespace SurubDirect.Tests;
public sealed class StoredCartTests(OrderingCartFixture fixture) : IClassFixture<OrderingCartFixture>
{
    [Fact]
    public async Task Persists_selection_across_scopes_and_isolates_owners()
    {
        var hash = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var line = new CartSelection("surub-m6", Guid.NewGuid(), 2);
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IShoppingCartRepository>();
            var cart = await repository.Create(hash, default);
            await repository.Replace(hash, cart.Id, cart.Version, [line], default);
        }

        await using var read = fixture.Services.CreateAsyncScope();
        var repo = read.ServiceProvider.GetRequiredService<IShoppingCartRepository>();
        var saved = await repo.Find(hash, default);
        Assert.Equal(2, saved!.Version);
        Assert.Equal(line, Assert.Single(saved.Lines));
        Assert.Null(await repo.Find(new string ('0', 64), default));
        var exception = await Assert.ThrowsAsync<BusinessException>(() => repo.Replace(new string ('0', 64), saved.Id, saved.Version, [], default));
        Assert.Equal(401, exception.Status);
    }

    [Fact]
    public async Task Concurrent_writers_have_one_winner_and_retry_cannot_overwrite()
    {
        var hash = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        await using var setup = fixture.Services.CreateAsyncScope();
        var repo = setup.ServiceProvider.GetRequiredService<IShoppingCartRepository>();
        var cart = await repo.Create(hash, default);
        async Task<bool> Save(int packs)
        {
            await using var scope = fixture.Services.CreateAsyncScope();
            try
            {
                await scope.ServiceProvider.GetRequiredService<IShoppingCartRepository>().Replace(hash, cart.Id, 1, [new("surub", Guid.NewGuid(), packs)], default);
                return true;
            }
            catch (BusinessException error)
            {
                Assert.Equal(409, error.Status);
                return false;
            }
        }

        var results = await Task.WhenAll(Save(2), Save(3));
        Assert.Single(results, x => x);
        var saved = await repo.Find(hash, default);
        Assert.Equal(2, saved!.Version);
        var error = await Assert.ThrowsAsync<BusinessException>(() => repo.Replace(hash, cart.Id, 1, [], default));
        Assert.Equal(409, error.Status);
        Assert.Single((await repo.Find(hash, default))!.Lines);
    }
}
