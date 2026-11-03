using SurubDirect.Ordering.Domain;
namespace SurubDirect.Ordering.Application;
public interface IDeliveryRates
{
    IReadOnlyList<DeliveryCharge> Options { get; }
}
