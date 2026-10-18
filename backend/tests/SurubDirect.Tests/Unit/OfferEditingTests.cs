using SurubDirect.BuildingBlocks;
using SurubDirect.Catalog.Domain;
using Xunit;

namespace SurubDirect.Tests;
public class OfferEditingTests
{
    [Fact]
    public void Pack_size_change_returns_product_to_draft_without_replacing_offer()
    {
        var product = Product.Create("Produs", "produs", "Categorie", "Descriere");
        var variant = product.AddVariant("SKU", "M8", 8, 40, "Oțel", "Zincat");
        var offer = variant.AddOffer("BOX", "Cutie 100", 100, 1, 1);
        var offerId = offer.Id;
        product.Publish();
        Assert.False(product.UpdateOffer(variant.Id, offerId, "Ambalaj 100", 100, 2, 2));
        Assert.Equal(ProductStatus.Published, product.Status);
        Assert.True(product.UpdateOffer(variant.Id, offerId, "Cutie 50", 50, 2, 2));
        Assert.Equal(ProductStatus.Draft, product.Status);
        Assert.Equal(offerId, offer.Id);
        Assert.Equal("BOX", offer.Code);
        Assert.Equal(variant.Id, offer.VariantId);
        Assert.Equal(200, offer.BaseUnits(4));
        Assert.Throws<BusinessException>(() => offer.BaseUnits(3));
        product.Publish();
        Assert.False(product.UpdateOffer(variant.Id, offerId, "Cutie 50", 50, 2, 2));
        Assert.Equal(ProductStatus.Published, product.Status);
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(10, 0, 1)]
    [InlineData(10, 1, 0)]
    [InlineData(-1, 1, 1)]
    [InlineData(int.MaxValue, 2, 1)]
    public void Invalid_quantities_do_not_change_offer_or_publication_status(int units, int min, int step)
    {
        var product = Product.Create("Produs", "produs", "Categorie", "Descriere");
        var variant = product.AddVariant("SKU", "M8", 8, 40, "Oțel", "Zincat");
        var offer = variant.AddOffer("BOX", "Original", 100, 1, 1);
        product.Publish();
        Assert.Throws<BusinessException>(() => product.UpdateOffer(variant.Id, offer.Id, "Changed", units, min, step));
        Assert.Equal(ProductStatus.Published, product.Status);
        Assert.Equal("Original", offer.Label);
        Assert.Equal(100, offer.UnitsPerPack);
        Assert.Equal(1, offer.MinPacks);
        Assert.Equal(1, offer.PackStep);
    }
}
