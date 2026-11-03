using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Domain;
namespace SurubDirect.Tests;
public sealed class TestDeliveryRates : IDeliveryRates
{
    public IReadOnlyList<DeliveryCharge> Options { get; } = new DeliveryCharge[]
    {
        new("demo-standard", "Livrare standard demonstrativă", "RON", 16.53m, 20m),
        new("demo-express", "Livrare expres demonstrativă", "RON", 24.79m, 30m)
    };
}
