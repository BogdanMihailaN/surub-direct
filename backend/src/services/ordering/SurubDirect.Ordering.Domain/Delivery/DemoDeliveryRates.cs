using SurubDirect.BuildingBlocks;
namespace SurubDirect.Ordering.Domain;

// Illustrative rates for the Development-only preparation flow, not carrier quotations.
public static class DemoDeliveryRates
{
    public static IReadOnlyList<DeliveryCharge> Options =>
    [
        new("demo-standard", "Livrare standard demonstrativă", "RON", 16.53m, 20m),
        new("demo-express", "Livrare expres demonstrativă", "RON", 24.79m, 30m)
    ];

    public static DeliveryCharge? Select(string? code, string currency, string country)
    {
        if (code is null) return null;
        var rate = Options.SingleOrDefault(x => x.Code == code)
            ?? throw new BusinessException("Metoda de livrare nu este disponibilă.");
        if (currency != rate.Currency || country != "RO")
            throw new BusinessException("Transportul demonstrativ este disponibil numai în RON pentru România.");
        return rate;
    }
}
