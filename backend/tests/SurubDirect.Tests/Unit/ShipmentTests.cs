using SurubDirect.BuildingBlocks;
using Xunit;
namespace SurubDirect.Tests;
public sealed class ShipmentTests
{
    [Fact]
    public void Shipment_requires_preparation_transport_payment_and_blocks_cancellation_after_dispatch()
    {
        var (order, _) = PaymentSimulationSamples.Create();
        var now = DateTimeOffset.UtcNow;
        Assert.Throws<BusinessException>(() => order.ChangeShipment(Guid.NewGuid(), 0, "Ready", "Test shipping", "admin", null, now));
        order.ChangeProcessing(Guid.NewGuid(), 0, "Processing", "Prepare order", "admin", now);
        order.ChangeProcessing(Guid.NewGuid(), 1, "Ready", "Prepare order", "admin", now);
        order.ChangeShipment(Guid.NewGuid(), 0, "Ready", "Test shipping", "admin", null, now);
        Assert.StartsWith("DEMO-", order.ShipmentReference);
        Assert.Throws<BusinessException>(() => order.ChangeProcessing(Guid.NewGuid(), 2, "Processing", "Prepare again", "admin", now));
        foreach (var payment in new[] { "NotStarted", "Pending", "Failed", "Refunded" })
            Assert.Throws<BusinessException>(() => order.ChangeShipment(Guid.NewGuid(), 1, "Shipped", "Test shipping", "admin", payment, now));
        var reference = order.ShipmentReference;
        order.ChangeShipment(Guid.NewGuid(), 1, "Shipped", "Test shipping", "admin", "Paid", now);
        Assert.Throws<BusinessException>(() => order.Cancel("Cancel shipped", "admin", now));
        order.ChangeShipment(Guid.NewGuid(), 2, "Delivered", "Test shipping", "admin", "Refunded", now);
        Assert.Equal(reference, order.ShipmentReference);
        Assert.Equal(3, order.ShipmentVersion);
        Assert.Throws<BusinessException>(() => order.Cancel("Cancel delivered", "admin", now));
        Assert.Throws<BusinessException>(() => order.ChangeShipment(Guid.NewGuid(), 3, "Ready", "Test shipping", "admin", "Paid", now));
    }

    [Theory]
    [InlineData(0, "Shipped")]
    [InlineData(0, "Delivered")]
    [InlineData(1, "Ready")]
    public void Invalid_or_stale_transitions_do_not_mutate(long version, string target)
    {
        var (order, _) = PaymentSimulationSamples.Create();
        var now = DateTimeOffset.UtcNow;
        order.ChangeProcessing(Guid.NewGuid(), 0, "Processing", "Prepare order", "admin", now);
        order.ChangeProcessing(Guid.NewGuid(), 1, "Ready", "Prepare order", "admin", now);
        Assert.Throws<BusinessException>(() => order.ChangeShipment(Guid.NewGuid(), version, target, "Test shipping", "admin", "Paid", now));
        Assert.Equal("NotStarted", order.ShipmentStatus);
        Assert.Null(order.ShipmentReference);
        Assert.Equal(0, order.ShipmentVersion);
    }
}
