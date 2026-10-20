using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Domain;
using Xunit;
namespace SurubDirect.Tests;

public sealed class PurchaseOrderClosureTests
{
    private static (PurchaseOrder Order, Guid Line) Sent()
    {
        var supplier = Supplier.Create("Supplier", "RO", "123", "", "", "", "", "");
        var item = supplier.AddItem(Guid.NewGuid(), "SKU", "M6", "EXT", "Box", 100, 10, "RON");
        var order = PurchaseOrder.Create(supplier, "RON", ""); var line = order.AddLine(supplier, item.Id, 1); order.MarkSent(supplier); return (order, line.Id);
    }
    [Fact]
    public void Closure_requires_a_reason_for_unreceived_units_and_preserves_snapshots()
    {
        var (o, line) = Sent(); Assert.Throws<BusinessException>(() => o.Close(" ", "admin"));
        var r = o.Receive(Guid.NewGuid(), line, 40, "REC", "admin"); o.CompleteReceipt(r.Id);
        var c = o.CorrectReceipt(Guid.NewGuid(), r.Id, 10, "COR", "Wrong quantity", "admin"); o.CompleteCorrection(c.Id);
        Assert.Throws<BusinessException>(() => o.Close("", "admin"));
        o.Close(" Supplier cannot deliver ", "admin");
        Assert.Equal(PurchaseOrderStatus.Closed, o.Status); Assert.Equal("Supplier cannot deliver", o.ClosureReason); Assert.Equal("admin", o.ClosedBy); Assert.NotNull(o.ClosedAt);
        Assert.Equal(100, Assert.Single(o.Lines).BaseUnits); Assert.Equal(40, Assert.Single(o.Receipts).Quantity); Assert.Equal(10, Assert.Single(o.Corrections).Quantity);
        Assert.Throws<BusinessException>(() => o.Receive(Guid.NewGuid(), line, 1, "NEW", "admin"));
        Assert.Throws<BusinessException>(() => o.CorrectReceipt(Guid.NewGuid(), r.Id, 1, "NEW", "Count", "admin"));
        Assert.Throws<BusinessException>(() => o.Cancel("Cancel")); Assert.Throws<BusinessException>(() => o.Close("Again", "admin"));
    }
    [Fact]
    public void Fully_received_order_can_close_without_reason_but_pending_operations_block_closure()
    {
        var (o, line) = Sent(); var r = o.Receive(Guid.NewGuid(), line, 100, "REC", "admin");
        Assert.Throws<BusinessException>(() => o.Close("Closing", "admin")); o.CompleteReceipt(r.Id);
        o.Close(null, "admin"); Assert.Equal("", o.ClosureReason);
        var (other, otherLine) = Sent(); var receipt = other.Receive(Guid.NewGuid(), otherLine, 100, "REC", "admin"); other.CompleteReceipt(receipt.Id);
        other.CorrectReceipt(Guid.NewGuid(), receipt.Id, 1, "COR", "Count", "admin");
        Assert.Throws<BusinessException>(() => other.Close("Closing", "admin")); Assert.Null(other.ClosedAt);
    }
    [Fact]
    public void Draft_cancelled_and_large_multi_line_orders_have_defined_closure_rules()
    {
        var supplier = Supplier.Create("Supplier", "RO", "123", "", "", "", "", ""); var o = PurchaseOrder.Create(supplier, "RON", "");
        Assert.Throws<BusinessException>(() => o.Close("Reason", "admin"));
        for (var i = 0; i < 2; i++) { var item = supplier.AddItem(Guid.NewGuid(), "SKU" + i, "M6", "EXT" + i, "Bulk", 1000000, 1, "RON"); o.AddLine(supplier, item.Id, 2000); }
        o.MarkSent(supplier); o.Close("No delivery", "admin"); Assert.Equal(PurchaseOrderStatus.Closed, o.Status);
        var (cancelled, _) = Sent(); cancelled.Cancel("Cancel"); Assert.Throws<BusinessException>(() => cancelled.Close("Reason", "admin"));
    }
}
