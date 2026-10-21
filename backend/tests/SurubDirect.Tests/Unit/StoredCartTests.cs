using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Domain;
using Xunit;

namespace SurubDirect.Tests.Unit;
public sealed class StoredCartTests
{
    [Fact]
    public void Rejects_expired_cart_even_with_current_version()
    {
        var now = DateTimeOffset.UtcNow;
        var cart = ShoppingCart.Create(new string ('a', 64), now);
        Assert.Equal(410, Assert.Throws<BusinessException>(() => cart.Replace(cart.Id, 1, [], now.AddDays(30))).Status);
        Assert.Equal(1, cart.Version);
    }

    [Fact]
    public void Rejects_different_cart_id_and_duplicate_offers_without_mutation()
    {
        var now = DateTimeOffset.UtcNow;
        var cart = ShoppingCart.Create(new string ('a', 64), now);
        Assert.Equal(409, Assert.Throws<BusinessException>(() => cart.Replace(Guid.NewGuid(), 1, [], now)).Status);
        var line = new CartSelection("surub", Guid.NewGuid(), 1);
        Assert.Throws<BusinessException>(() => cart.Replace(cart.Id, 1, [line, line], now));
        Assert.Throws<BusinessException>(() => cart.Replace(cart.Id, 1, [line with { Packs = 0 }], now));
        Assert.Throws<BusinessException>(() => cart.Replace(cart.Id, 1, Enumerable.Range(0, 51).Select(_ => line with { OfferId = Guid.NewGuid() }).ToList(), now));
        Assert.Empty(cart.Lines);
        Assert.Equal(1, cart.Version);
    }
}
