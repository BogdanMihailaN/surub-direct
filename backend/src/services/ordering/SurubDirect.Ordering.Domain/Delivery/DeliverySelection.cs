using SurubDirect.BuildingBlocks;
namespace SurubDirect.Ordering.Domain;

// Pure selection rules; rates are supplied by the owning service.
public static class DeliverySelection
{
    public static DeliveryCharge? Select(IReadOnlyList<DeliveryCharge> options, string? code, string currency, string country)
    {
        if (code is null) return null;
        var rate = options.SingleOrDefault(x => x.Code == code)
            ?? throw new BusinessException("Metoda de livrare nu este disponibilă.");
        if (currency != rate.Currency || country != "RO")
            throw new BusinessException("Transportul demonstrativ este disponibil numai în RON pentru România.");
        return rate;
    }
}
