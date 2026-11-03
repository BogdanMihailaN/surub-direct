using SurubDirect.Ordering.Contracts;
using SurubDirect.Ordering.Domain;
namespace SurubDirect.Ordering.Application;
public sealed class GetDeliveryOptionsHandler
{
    public IReadOnlyList<DeliveryChargeDto> Handle() => DemoDeliveryRates.Options.Select(x => DeliveryChargeProjection.Map(x)!).ToArray();
}
