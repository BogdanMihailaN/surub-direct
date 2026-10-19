using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Domain;
using Xunit;
namespace SurubDirect.Tests;
public sealed class SupplierTests
{
    private static Supplier New() => Supplier.Create("Furnizor", "ro", "RO 123456", "Contact", "test@example.com", "", "", "");
    [Fact]
    public void Fiscal_identity_is_normalized_and_failed_edits_are_atomic()
    {
        var supplier = New(); Assert.Equal("RO", supplier.CountryCode); Assert.Equal("123456", supplier.TaxId);
        Assert.Throws<BusinessException>(() => supplier.Update("Changed", "RO", "9999", "", "not-an-email", "", "", "", false));
        Assert.Equal("Furnizor", supplier.Name); Assert.True(supplier.IsActive);
    }
    [Fact]
    public void Inactivation_preserves_items_and_reactivation_allows_edits()
    {
        var supplier = New(); var variant = Guid.NewGuid();
        var item = supplier.AddItem(variant, "SKU", "M6", "a-1", "Cutie", 100, 8.75m, "eur");
        Assert.Equal("A-1", item.SupplierCode); Assert.Equal("EUR", item.Currency);
        supplier.Update("Furnizor", "RO", "123456", "", "", "", "", "", false);
        Assert.Single(supplier.Items);
        Assert.Throws<BusinessException>(() => supplier.AddItem(Guid.NewGuid(), "B", "M8", "b", "Cutie", 1, 1, "RON"));
        Assert.Throws<BusinessException>(() => supplier.UpdateItem(item.Id, "a-2", "Pungă", 10, 1, "RON", true));
        supplier.Update("Furnizor", "RO", "123456", "", "", "", "", "", true);
        supplier.UpdateItem(item.Id, "a-2", "Pungă", 10, 1, "RON", false);
        Assert.Equal(variant, item.VariantId); Assert.Equal("SKU", item.Sku); Assert.False(item.IsActive);
    }
    [Fact]
    public void Duplicate_skus_codes_and_foreign_item_ids_are_rejected()
    {
        var supplier = New(); var variant = Guid.NewGuid();
        supplier.AddItem(variant, "A", "M6", "a", "Cutie", 1, 1, "RON");
        Assert.Throws<BusinessException>(() => supplier.AddItem(variant, "A", "M6", "b", "Cutie", 1, 1, "RON"));
        Assert.Throws<BusinessException>(() => supplier.AddItem(Guid.NewGuid(), "B", "M8", "A", "Cutie", 1, 1, "RON"));
        Assert.Throws<BusinessException>(() => supplier.UpdateItem(Guid.NewGuid(), "a", "Cutie", 1, 1, "RON", true));
        Assert.Single(supplier.Items);
    }
    [Theory]
    [InlineData(0, "1", "RON")]
    [InlineData(1, "0", "RON")]
    [InlineData(1, "1000001", "RON")]
    [InlineData(1, "1.0000001", "RON")]
    [InlineData(1, "1", "ABC")]
    public void Invalid_purchase_amounts_are_rejected(int units, string cost, string currency)
    {
        var supplier = New();
        Assert.Throws<BusinessException>(() => supplier.AddItem(Guid.NewGuid(), "A", "M6", "a", "Cutie", units, decimal.Parse(cost, System.Globalization.CultureInfo.InvariantCulture), currency));
        Assert.Empty(supplier.Items);
    }
}
