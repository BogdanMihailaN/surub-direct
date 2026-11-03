namespace SurubDirect.Ordering.Domain;

public sealed record DeliveryCharge(string Code, string Label, string Currency, decimal NetAmount, decimal GrossAmount);
