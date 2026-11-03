using SurubDirect.Ordering.Domain;
namespace SurubDirect.Tests;
public static class ReturnSamples
{
    public static (DemoOrder Order, ReservationPlan Plan) Create()
    {
        var (order, plan) = PaymentSimulationSamples.Create();
        var now = DateTimeOffset.UtcNow;
        order.ChangeProcessing(Guid.NewGuid(), 0, "Processing", "Prepare order", "admin", now);
        order.ChangeProcessing(Guid.NewGuid(), 1, "Ready", "Prepare order", "admin", now);
        order.ChangeShipment(Guid.NewGuid(), 0, "Ready", "Prepare shipment", "admin", null, now);
        order.ChangeShipment(Guid.NewGuid(), 1, "Shipped", "Dispatch shipment", "admin", "Paid", now);
        order.ChangeShipment(Guid.NewGuid(), 2, "Delivered", "Deliver shipment", "admin", "Paid", now);
        return (order, plan);
    }
}
