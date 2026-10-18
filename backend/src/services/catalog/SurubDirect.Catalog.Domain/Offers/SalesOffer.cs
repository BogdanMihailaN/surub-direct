using SurubDirect.BuildingBlocks;

namespace SurubDirect.Catalog.Domain;
public sealed class SalesOffer
{
    private SalesOffer()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid VariantId { get; private set; }
    public string Code { get; private set; } = "";
    public string Label { get; private set; } = "";
    public int UnitsPerPack { get; private set; }
    public int MinPacks { get; private set; }
    public int PackStep { get; private set; }

    public static SalesOffer Create(Guid variantId, string code, string label, int units, int min, int step)
    {
        var offer = new SalesOffer
        {
            VariantId = variantId,
            Code = Guard.Text(code, "Cod ofertă", 80).ToUpperInvariant()
        };
        offer.UpdateDetails(label, units, min, step);
        return offer;
    }

    public void UpdateDetails(string label, int units, int min, int step)
    {
        Guard.Positive(units, "Bucăți/ambalaj");
        Guard.Positive(min, "Cantitate minimă");
        Guard.Positive(step, "Pas cantitate");
        if ((long)units * min > int.MaxValue)
            throw new BusinessException("Cantitatea minimă în bucăți depășește limita acceptată.");
        var validLabel = Guard.Text(label, "Ambalaj", 100);
        Label = validLabel;
        UnitsPerPack = units;
        MinPacks = min;
        PackStep = step;
    }

    public int BaseUnits(int packs)
    {
        if (packs < MinPacks || (packs - MinPacks) % PackStep != 0)
            throw new BusinessException("Cantitate incompatibilă cu ambalajul.");
        return checked(packs * UnitsPerPack);
    }
}
