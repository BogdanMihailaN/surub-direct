using SurubDirect.BuildingBlocks;

namespace SurubDirect.Pricing.Domain;
public sealed class OfferPrice
{
    private OfferPrice()
    {
    }

    public Guid OfferId { get; private set; }
    public decimal NetAmount { get; private set; }
    public decimal TaxRate { get; private set; }
    public string Currency { get; private set; } = "RON";
    public DateTimeOffset UpdatedAt { get; private set; }

    public static OfferPrice Create(Guid id, decimal net, decimal tax)
    {
        var p = new OfferPrice
        {
            OfferId = id
        };
        p.Change(net, tax);
        return p;
    }

    public void Change(decimal net, decimal tax)
    {
        if (net <= 0 || net > 1000000 || decimal.Round(net, 6) != net)
            throw new BusinessException("Prețul net trebuie să fie pozitiv, cel mult 1.000.000, cu maximum 6 zecimale.");
        if (tax < 0 || tax > 1 || decimal.Round(tax, 4) != tax)
            throw new BusinessException("TVA trebuie exprimat între 0 și 1, cu maximum 4 zecimale.");
        NetAmount = net;
        TaxRate = tax;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public decimal GrossAmount => decimal.Round(NetAmount * (1 + TaxRate), 2, MidpointRounding.AwayFromZero);
}
