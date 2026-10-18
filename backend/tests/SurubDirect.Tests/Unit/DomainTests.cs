using Xunit;
using SurubDirect.BuildingBlocks;
using SurubDirect.Catalog.Domain;
using SurubDirect.Pricing.Domain;
using SurubDirect.Inventory.Domain;

namespace SurubDirect.Tests;
public class DomainTests
{
    [Fact]
    public void Offer_converts_packs_and_enforces_minimum_and_step()
    {
        var offer = SalesOffer.Create(Guid.NewGuid(), "TEST", "Cutie", 100, 2, 2);
        Assert.Equal(400, offer.BaseUnits(4));
        Assert.Throws<BusinessException>(() => offer.BaseUnits(1));
        Assert.Throws<BusinessException>(() => offer.BaseUnits(3));
    }

    [Fact]
    public void Pack_conversion_rejects_integer_overflow()
    {
        var offer = SalesOffer.Create(Guid.NewGuid(), "BIG", "Cutie", int.MaxValue, 1, 1);
        Assert.Throws<OverflowException>(() => offer.BaseUnits(2));
    }

    [Fact]
    public void Product_cannot_publish_without_an_offer()
    {
        var p = Product.Create("Produs", "produs", "Categorie", "Descriere");
        Assert.Throws<BusinessException>(() => p.Publish());
        p.AddVariant("SKU", "M8", 8, null, "Oțel", "Zincat");
        Assert.Throws<BusinessException>(() => p.Publish());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(10000001)]
    public void Receipt_rejects_invalid_quantities(int quantity)
    {
        Assert.Throws<BusinessException>(() => StockReceipt.Create(Guid.NewGuid(), Guid.NewGuid(), quantity, "REF", "Admin"));
    }

    [Fact]
    public void Price_uses_decimal_rounding_and_validates_precision()
    {
        Assert.Equal(1.01m, OfferPrice.Create(Guid.NewGuid(), 1.005m, 0).GrossAmount);
        Assert.Equal(54.45m, OfferPrice.Create(Guid.NewGuid(), 45m, 0.21m).GrossAmount);
        Assert.Throws<BusinessException>(() => OfferPrice.Create(Guid.NewGuid(), 0.0000001m, 0.21m));
    }

    [Theory]
    [InlineData("M8 × 40", "m8x40")]
    [InlineData("Șurub M8 x 40", "surubm8x40")]
    public void Search_normalizes_romanian_text_and_dimensions(string input, string expected) => Assert.Equal(expected, Product.Normalize(input));
}
