namespace SurubDirect.Ordering.Infrastructure.Delivery;
public sealed class DeliveryRateSettings
{
    public string Code { get; set; } = "";
    public string Label { get; set; } = "";
    public string Currency { get; set; } = "RON";
    public decimal NetAmount { get; set; }
    public decimal GrossAmount { get; set; }
    public bool Enabled { get; set; } = true;
}
