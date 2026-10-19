using SurubDirect.BuildingBlocks;
using SurubDirect.Catalog.Domain;
using Xunit;

namespace SurubDirect.Tests;
public class TaxonomyTests
{
    [Fact]
    public void Hierarchy_rejects_cycles_missing_parents_and_excessive_depth()
    {
        var root = Category.Create("Root", "root", null, 0);
        var child = Category.Create("Child", "child", root.Id, 0);
        root.Update("Root", child.Id, 0);
        Assert.Throws<BusinessException>(() => CategoryHierarchy.Validate([root, child]));
        root.Update("Root", null, 0);
        Assert.Equal(404, Assert.Throws<BusinessException>(() => CategoryHierarchy.Validate([child])).Status);
        var nodes = new List<Category> { root, child };
        for (var i = 2; i < 5; i++) nodes.Add(Category.Create("Level " + i, "level-" + i, nodes[^1].Id, 0));
        CategoryHierarchy.Validate(nodes);
        nodes.Add(Category.Create("Too deep", "too-deep", nodes[^1].Id, 0));
        Assert.Throws<BusinessException>(() => CategoryHierarchy.Validate(nodes));
    }

    [Fact]
    public void Attribute_limit_includes_inherited_definitions_and_deduplicates_them()
    {
        var root = Category.Create("Root", "root", null, 0);
        var child = Category.Create("Child", "child", root.Id, 0);
        var ids = Enumerable.Range(0, 50).Select(_ => Guid.NewGuid()).ToArray();
        root.SetAttributes(ids);
        child.SetAttributes(ids);
        CategoryHierarchy.Validate([root, child]);
        child.SetAttributes([Guid.NewGuid()]);
        Assert.Throws<BusinessException>(() => CategoryHierarchy.Validate([root, child]));
    }

    [Fact]
    public void Values_enforce_types_precision_and_atomic_variant_updates()
    {
        var number = TechnicalAttribute.Create("pitch", "Pas", AttributeKind.Number, "mm", 0);
        var text = TechnicalAttribute.Create("standard", "Standard", AttributeKind.Text, null, 0);
        var variant = Variant.Create(Guid.NewGuid(), "SKU", "M8", 8, 40, "Oțel", "Zincat");
        foreach (var invalid in new[] { decimal.MinValue, 1000000000000m, 1.1234567m })
            Assert.Throws<BusinessException>(() => VariantAttributeValue.Create(variant.Id, number, null, invalid));
        Assert.Throws<BusinessException>(() => VariantAttributeValue.Create(variant.Id, number, "1", 1));
        Assert.Throws<BusinessException>(() => VariantAttributeValue.Create(variant.Id, text, "DIN", 1));
        var value = VariantAttributeValue.Create(variant.Id, number, null, 1.25m);
        variant.SetAttributes([number.Id], [value]);
        Assert.Throws<BusinessException>(() => variant.SetAttributes([number.Id], [value, value]));
        Assert.Throws<BusinessException>(() => variant.SetAttributes([text.Id], [value]));
        Assert.Equal(1.25m, Assert.Single(variant.AttributeValues).NumberValue);
        variant.SetAttributes([text.Id], [VariantAttributeValue.Create(variant.Id, text, " din 933 ", null)]);
        Assert.Equal(2, variant.AttributeValues.Count);
        Assert.Equal("DIN 933", variant.AttributeValues.Single(x => x.AttributeId == text.Id).NormalizedText);
        variant.SetAttributes([number.Id], []);
        Assert.Equal(text.Id, Assert.Single(variant.AttributeValues).AttributeId);
    }
}
