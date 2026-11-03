namespace SurubDirect.Ordering.Contracts;
public sealed record DeliveryChargeDto(string Code, string Label, string Currency, decimal NetAmount, decimal GrossAmount);
