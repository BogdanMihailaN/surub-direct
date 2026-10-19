using SurubDirect.BuildingBlocks;
using SurubDirect.Catalog.Domain;
using Xunit;
namespace SurubDirect.Tests;
public class ProductImageTests
{
    [Fact]
    public void Gallery_enforces_ownership_order_primary_and_archive_rules()
    {
        var product = Product.Create("Test", "test", "Test", "Description");
        var variant = product.AddVariant("SKU", "M8", 8, 40, "Oțel", "Zincat");
        var first = product.AddImage(Guid.NewGuid(), "Vedere laterală", null, 800, 600);
        var second = product.AddImage(Guid.NewGuid(), "Detaliu M8", variant.Id, 600, 400);
        Assert.True(first.IsPrimary);
        Assert.False(second.IsPrimary);
        Assert.Throws<BusinessException>(() => product.ArrangeImages([first.Id, first.Id], first.Id));
        Assert.Throws<BusinessException>(() => product.ArrangeImages([first.Id], first.Id));
        Assert.Equal(404, Assert.Throws<BusinessException>(() => product.UpdateImage(first.Id, "Changed", Guid.NewGuid())).Status);
        Assert.Equal("Vedere laterală", first.AltText);
        Assert.Equal(404, Assert.Throws<BusinessException>(() => product.RemoveImage(Guid.NewGuid())).Status);
        product.ArrangeImages([second.Id, first.Id], second.Id);
        Assert.True(second.IsPrimary);
        Assert.Equal(0, second.SortOrder);
        product.RemoveImage(second.Id);
        Assert.True(Assert.Single(product.Images).IsPrimary);
        Assert.Equal(0, first.SortOrder);
        product.Archive();
        Assert.Equal(409, Assert.Throws<BusinessException>(() => product.RemoveImage(first.Id)).Status);
        Assert.Equal(409, Assert.Throws<BusinessException>(() => product.AddImage(Guid.NewGuid(), "No", null, 1, 1)).Status);
    }
    [Fact]
    public void Gallery_limits_and_alt_text_are_validated_before_mutation()
    {
        var product = Product.Create("Test", "test", "Test", "Description");
        Assert.Throws<BusinessException>(() => product.AddImage(Guid.NewGuid(), " ", null, 1, 1));
        Assert.Empty(product.Images);
        for (var i = 0; i < 12; i++) product.AddImage(Guid.NewGuid(), "Image " + i, null, 10, 10);
        Assert.Throws<BusinessException>(() => product.AddImage(Guid.NewGuid(), "Extra", null, 1, 1));
        Assert.Equal(12, product.Images.Count);
        Assert.Single(product.Images, x => x.IsPrimary);
    }
}
