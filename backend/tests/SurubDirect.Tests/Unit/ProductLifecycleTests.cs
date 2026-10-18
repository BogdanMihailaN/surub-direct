using SurubDirect.BuildingBlocks;
using SurubDirect.Catalog.Domain;
using Xunit;

namespace SurubDirect.Tests;
public class ProductLifecycleTests
{
    [Fact]
    public void Invalid_edit_does_not_partially_change_the_product()
    {
        var product = Product.Create("Original", "original", "Categorie", "Descriere");
        Assert.Throws<BusinessException>(() => product.UpdateDetails("Nume nou", "Categorie nouă", " "));
        Assert.Equal("Original", product.Name);
        Assert.Equal("Categorie", product.Category);
        Assert.Equal("Descriere", product.Description);
    }

    [Fact]
    public void Archive_is_repeatable_and_restore_always_returns_a_draft()
    {
        var product = Product.Create("Original", "original", "Categorie", "Descriere");
        var variant = product.AddVariant("SKU", "M8", 8, 40, "Oțel", "Zincat");
        variant.AddOffer("CUTIE", "Cutie", 100, 1, 1);
        product.Publish();
        product.Archive();
        product.Archive();
        Assert.Equal(ProductStatus.Archived, product.Status);
        Assert.Throws<BusinessException>(() => product.Publish());
        Assert.Throws<BusinessException>(() => product.UpdateDetails("Nume", "Categorie", "Descriere"));
        Assert.Throws<BusinessException>(() => product.AddVariant("ALT", "M10", 10, 40, "Oțel", "Zincat"));
        product.Restore();
        product.Restore();
        Assert.Equal(ProductStatus.Draft, product.Status);
        Assert.Same(variant, Assert.Single(product.Variants));
        Assert.Single(variant.Offers);
        product.Publish();
        Assert.Throws<BusinessException>(() => product.Restore());
        Assert.Equal(ProductStatus.Published, product.Status);
    }
}
