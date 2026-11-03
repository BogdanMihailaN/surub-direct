using SurubDirect.Ordering.Contracts;
using SurubDirect.Ordering.Domain;
namespace SurubDirect.Ordering.Application;
public static class DeliveryChargeProjection
{
    public static DeliveryChargeDto? Map(DeliveryCharge? charge) => charge is null ? null
        : new(charge.Code, charge.Label, charge.Currency, charge.NetAmount, charge.GrossAmount);
}
