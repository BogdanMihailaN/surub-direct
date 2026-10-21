using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using Xunit;

namespace SurubDirect.Tests;
public sealed class CartCustomerTests(OrderingCartFixture fixture) : IClassFixture<OrderingCartFixture>
{
    [Fact]
    public async Task Customer_persists_isolated_and_is_removed_on_explicit_clear_or_empty_cart()
    {
        var hash = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        Guid id;
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IShoppingCartRepository>();
            var cart = await repo.Create(hash, default);
            id = cart.Id;
            await repo.Replace(hash, id, 1, [new("surub", Guid.NewGuid(), 2)], default);
            await repo.SaveCustomer(hash, id, 2, CustomerDraftSamples.Person(), default);
        }

        await using var read = fixture.Services.CreateAsyncScope();
        var repository = read.ServiceProvider.GetRequiredService<IShoppingCartRepository>();
        var saved = await repository.Find(hash, default);
        Assert.Equal(CustomerDraftSamples.Person(), saved!.Customer);
        Assert.Equal(3, saved.Version);
        Assert.Equal(401, (await Assert.ThrowsAsync<BusinessException>(() => repository.SaveCustomer(new string ('0', 64), id, 3, null, default))).Status);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => repository.Replace(hash, id, 2, [], default))).Status);
        var cleared = await repository.SaveCustomer(hash, id, 3, null, default);
        Assert.Null(cleared.Customer);
        await repository.SaveCustomer(hash, id, 4, CustomerDraftSamples.Person(), default);
        var empty = await repository.Replace(hash, id, 5, [], default);
        Assert.Null(empty.Customer);
        Assert.Null((await repository.Find(hash, default))!.Customer);
    }

    [Fact]
    public async Task Rejected_customer_does_not_change_saved_data_or_version()
    {
        var hash = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        await using var scope = fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IShoppingCartRepository>();
        var cart = await repo.Create(hash, default);
        await repo.Replace(hash, cart.Id, 1, [new("surub", Guid.NewGuid(), 1)], default);
        await repo.SaveCustomer(hash, cart.Id, 2, CustomerDraftSamples.Person(), default);
        await Assert.ThrowsAsync<BusinessException>(() => repo.SaveCustomer(hash, cart.Id, 3, CustomerDraftSamples.Person()with { Email = "bad" }, default));
        var saved = await repo.Find(hash, default);
        Assert.Equal(3, saved!.Version);
        Assert.Equal(CustomerDraftSamples.Person(), saved.Customer);
    }
}
