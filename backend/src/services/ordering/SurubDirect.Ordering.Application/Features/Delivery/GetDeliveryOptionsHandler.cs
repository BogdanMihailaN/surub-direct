using SurubDirect.Ordering.Contracts;
using SurubDirect.Ordering.Domain;
namespace SurubDirect.Ordering.Application;
public sealed class GetDeliveryOptionsHandler(IDeliveryRates rates)
{
    public IReadOnlyList<DeliveryChargeDto> Handle() => rates.Options.Select(x => DeliveryChargeProjection.Map(x)!).ToArray();
}
