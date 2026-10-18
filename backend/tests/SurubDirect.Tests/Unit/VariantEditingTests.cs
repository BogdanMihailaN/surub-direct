using SurubDirect.BuildingBlocks;
using SurubDirect.Catalog.Domain;
using Xunit;

namespace SurubDirect.Tests;
public class VariantEditingTests
{
    [Fact]
    public void Editing_preserves_identity_and_rebuilds_search_text()
    {
        var productId = Guid.NewGuid();
        var variant = Variant.Create(productId, "sku-fixed", "M8 × 40", 8, 40, "Oțel", "Zincat");
        var id = variant.Id;
        var offer = variant.AddOffer("BOX", "Cutie", 100, 1, 1);
        variant.UpdateDetails("  Șurub M10 × 65  ", 10, 65, " Inox A2 ", " Natural ");
        Assert.Equal(id, variant.Id);
        Assert.Equal(productId, variant.ProductId);
        Assert.Equal("SKU-FIXED", variant.Sku);
        Assert.Equal("Șurub M10 × 65", variant.Label);
        Assert.Equal("Inox A2", variant.Material);
        Assert.Equal("Natural", variant.Finish);
        Assert.Contains("surubm10x65", variant.SearchText);
        Assert.DoesNotContain("m8x40", variant.SearchText);
        Assert.Same(offer, Assert.Single(variant.Offers));
        variant.UpdateDetails("Fără dimensiuni", null, null, "Oțel", "Brut");
        Assert.Null(variant.DiameterMm);
        Assert.Null(variant.LengthMm);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("0.0001")]
    [InlineData("8.1234")]
    [InlineData("1000000000")]
    public void Invalid_dimensions_cannot_partially_mutate_a_variant(string input)
    {
        var invalid = decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture);
        var variant = Variant.Create(Guid.NewGuid(), "SKU", "Original", 8, 40, "Oțel", "Zincat");
        Assert.Throws<BusinessException>(() => variant.UpdateDetails("Changed", 10, invalid, "Inox", "Brut"));
        Assert.Equal("Original", variant.Label);
        Assert.Equal(8, variant.DiameterMm);
        Assert.Equal(40, variant.LengthMm);
        Assert.Equal("Oțel", variant.Material);
        Assert.Throws<BusinessException>(() => Variant.Create(Guid.NewGuid(), "NEW", "New", invalid, null, "Oțel", "Brut"));
    }
}
