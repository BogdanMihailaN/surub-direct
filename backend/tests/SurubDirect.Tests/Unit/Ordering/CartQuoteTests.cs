using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Domain;
using Xunit;
namespace SurubDirect.Tests;

public sealed class CartQuoteTests
{
    private static CartOfferSnapshot Offer() => new(Guid.NewGuid(), Guid.NewGuid(), "Screw", "SKU", "Box", 10, 2, 3, 100, 10m, 12.1m, "RON");
    [Fact]
    public async Task Current_server_prices_are_multiplied_per_pack_and_checkout_stays_disabled()
    {
        var o = Offer(); var result = await new QuoteCartHandler(new CartCatalogStub([o])).Handle(new([new("screw", o.OfferId, 5)]), default);
        Assert.Equal(50m, result.NetTotal); Assert.Equal(60.5m, result.GrossTotal); Assert.False(result.CheckoutEnabled); Assert.Equal(50, Assert.Single(result.Lines).BaseUnits);
    }
    [Fact]
    public async Task All_packaging_of_same_variant_shares_available_stock()
    {
        var a = Offer(); var b = a with { OfferId = Guid.NewGuid(), UnitsPerPack = 20, MinPacks = 1, PackStep = 1 }; var catalog = new CartCatalogStub([a, b]);
        var result = await new QuoteCartHandler(catalog).Handle(new([new("screw", a.OfferId, 5), new("screw", b.OfferId, 3)]), default);
        Assert.Null(result.GrossTotal); Assert.All(result.Lines, l => Assert.Contains("Stoc insuficient", l.Issue)); Assert.Equal(1, catalog.Calls);
    }
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(2147483647)]
    public async Task Invalid_minimum_step_and_overflow_hide_totals(int packs)
    {
        var o = Offer(); var result = await new QuoteCartHandler(new CartCatalogStub([o])).Handle(new([new("screw", o.OfferId, packs)]), default); Assert.Null(result.GrossTotal); Assert.NotNull(Assert.Single(result.Lines).Issue);
    }
    [Fact]
    public async Task Missing_archived_unpriced_and_mixed_currency_lines_prevent_a_valid_total()
    {
        var o = Offer(); var missing = await new QuoteCartHandler(new CartCatalogStub(null)).Handle(new([new("screw", o.OfferId, 2)]), default); Assert.Null(missing.GrossTotal);
        var unpriced = await new QuoteCartHandler(new CartCatalogStub([o with { Net = null, Gross = null, Currency = null }])).Handle(new([new("screw", o.OfferId, 2)]), default); Assert.Equal("Preț indisponibil.", Assert.Single(unpriced.Lines).Issue);
        var other = o with { OfferId = Guid.NewGuid(), VariantId = Guid.NewGuid(), Currency = "EUR" };
        var mixed = await new QuoteCartHandler(new CartCatalogStub([o, other])).Handle(new([new("screw", o.OfferId, 2), new("screw", other.OfferId, 2)]), default); Assert.Null(mixed.GrossTotal); Assert.All(mixed.Lines, l => Assert.NotNull(l.Issue));
    }
    [Fact]
    public async Task Malformed_and_duplicate_input_is_rejected_before_dependency_calls()
    {
        var o = Offer(); var catalog = new CartCatalogStub([o]); var handler = new QuoteCartHandler(catalog); var selection = new CartSelection("screw", o.OfferId, 2);
        await Assert.ThrowsAsync<BusinessException>(() => handler.Handle(new([selection, selection]), default));
        await Assert.ThrowsAsync<BusinessException>(() => handler.Handle(new([selection with { ProductSlug = "../internal" }]), default));
        await Assert.ThrowsAsync<BusinessException>(() => handler.Handle(new([selection with { Packs = 0 }]), default));
        await Assert.ThrowsAsync<BusinessException>(() => handler.Handle(new(Enumerable.Range(0, 51).Select(_ => selection with { OfferId = Guid.NewGuid() }).ToArray()), default)); Assert.Equal(0, catalog.Calls);
    }
}
