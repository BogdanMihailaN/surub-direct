using SurubDirect.Ordering.Domain;
namespace SurubDirect.Tests;
public static class PaymentSimulationSamples
{
    public static (DemoOrder Order, ReservationPlan Plan) Create(bool confirmed = true)
    {
        var now = DateTimeOffset.UtcNow;
        var cart = ShoppingCart.Create(new string('a', 64), now);
        var offer = Guid.NewGuid();
        var variant = Guid.NewGuid();
        var id = Guid.NewGuid();
        cart.Replace(cart.Id, cart.Version, [new("demo", offer, 2)], now);
        cart.SaveCustomer(cart.Id, cart.Version, CustomerDraftSamples.Person(), now);
        var snapshot = PreparedCart.Create(id, cart, [new(offer, variant, "Produs", "SKU", "Cutie", 2, 10, 10, 12.1m, 20, 24.2m)], "RON", now,
            new DeliveryCharge("demo", "Transport demo", "RON", 16.53m, 20m));
        var plan = ReservationPlan.Create(id, id, [new(variant, 20)], now);
        plan.Observe("Active", now.AddMinutes(15), now);
        plan.Confirm(now);
        var order = DemoOrder.Create(cart.OwnerHash, snapshot, now);
        if (confirmed) { plan.Observe("Consumed", now.AddMinutes(15), now); order.Reconcile(plan, now); }
        return (order, plan);
    }
}
