using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Domain;
using Xunit;
namespace SurubDirect.Tests;
public sealed class PurchaseOrderTests
{
    private static Supplier Supplier() => SurubDirect.Purchasing.Domain.Supplier.Create("Original", "RO", "123", "", "", "", "Address", "");
    [Fact]
    public void Receipt_validation_rejects_drafts_invalid_amounts_and_foreign_lines()
    {
        var supplier = Supplier(); var item = supplier.AddItem(Guid.NewGuid(), "SKU", "M6", "CODE", "Cutie", 100, 10, "RON");
        var order = PurchaseOrder.Create(supplier, "RON", ""); var line = order.AddLine(supplier, item.Id, 1);
        Assert.Throws<BusinessException>(() => order.Receive(Guid.NewGuid(), line.Id, 10, "Aviz", "admin"));
        order.MarkSent(supplier);
        Assert.Throws<BusinessException>(() => order.Receive(Guid.Empty, line.Id, 10, "Aviz", "admin"));
        Assert.Throws<BusinessException>(() => order.Receive(Guid.NewGuid(), line.Id, 0, "Aviz", "admin"));
        Assert.Throws<BusinessException>(() => order.Receive(Guid.NewGuid(), line.Id, 10000001, "Aviz", "admin"));
        Assert.Throws<BusinessException>(() => order.Receive(Guid.NewGuid(), Guid.NewGuid(), 10, "Aviz", "admin"));
        Assert.Throws<BusinessException>(() => order.Receive(Guid.NewGuid(), line.Id, 10, " ", "admin"));
        Assert.Empty(order.Receipts);
    }
    [Fact]
    public void Snapshots_survive_supplier_changes_and_sent_orders_are_immutable()
    {
        var supplier = Supplier(); var item = supplier.AddItem(Guid.NewGuid(), "SKU", "M6", "CODE", "Cutie", 100, 12.345m, "RON");
        var order = PurchaseOrder.Create(supplier, "RON", ""); var line = order.AddLine(supplier, item.Id, 3);
        supplier.UpdateItem(item.Id, "NEW", "Punga", 10, 99, "EUR", true);
        supplier.Update("Changed", "RO", "456", "", "", "", "New", "", true);
        Assert.Equal("Original", order.SupplierName); Assert.Equal(300, line.BaseUnits); Assert.Equal(37.04m, order.NetTotal);
        order.MarkSent(supplier); Assert.NotNull(order.SentAt);
        Assert.Throws<BusinessException>(() => order.UpdateLine(line.Id, 4, 1));
        Assert.Throws<BusinessException>(() => order.RemoveLine(line.Id));
        Assert.Throws<BusinessException>(() => order.UpdateNotes("changed"));
        order.Cancel("External cancellation"); Assert.Equal(PurchaseOrderStatus.Cancelled, order.Status); Assert.Single(order.Lines);
        Assert.Throws<BusinessException>(() => order.MarkSent(supplier));
        Assert.Throws<BusinessException>(() => order.Cancel("Again"));
    }
    [Fact]
    public void Invalid_quantities_currency_duplicates_and_inactive_items_are_rejected()
    {
        var supplier = Supplier(); var item = supplier.AddItem(Guid.NewGuid(), "SKU", "M6", "CODE", "Cutie", 100, 10, "RON");
        var order = PurchaseOrder.Create(supplier, "RON", "");
        Assert.Throws<BusinessException>(() => order.MarkSent(supplier));
        Assert.Throws<BusinessException>(() => order.AddLine(supplier, item.Id, 0));
        Assert.Throws<BusinessException>(() => order.AddLine(supplier, item.Id, int.MaxValue));
        var line = order.AddLine(supplier, item.Id, 1);
        Assert.Throws<BusinessException>(() => order.AddLine(supplier, item.Id, 1));
        Assert.Throws<BusinessException>(() => order.UpdateLine(line.Id, 1, 0.0000001m));
        Assert.Throws<BusinessException>(() => order.EnsureVersion(1));
        supplier.UpdateItem(item.Id, "CODE", "Cutie", 100, 10, "RON", false);
        Assert.Throws<BusinessException>(() => order.MarkSent(supplier));
        var other = PurchaseOrder.Create(supplier, "EUR", "");
        supplier.UpdateItem(item.Id, "CODE", "Cutie", 100, 10, "RON", true);
        Assert.Throws<BusinessException>(() => other.AddLine(supplier, item.Id, 1));
    }
}
