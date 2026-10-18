using SurubDirect.BuildingBlocks;

namespace SurubDirect.Catalog.Domain;
public sealed class Variant
{
    private Variant()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProductId { get; private set; }
    public string Sku { get; private set; } = "";
    public string Label { get; private set; } = "";
    public decimal? DiameterMm { get; private set; }
    public decimal? LengthMm { get; private set; }
    public string Material { get; private set; } = "";
    public string Finish { get; private set; } = "";
    public string SearchText { get; private set; } = "";
    public List<VariantAttributeValue> AttributeValues { get; private set; } = [];
    public List<SalesOffer> Offers { get; private set; } = [];

    public static Variant Create(Guid productId, string sku, string label, decimal? diameter, decimal? length, string material, string finish)
    {
        var variant = new Variant
        {
            ProductId = productId,
            Sku = Guard.Text(sku, "SKU", 80).ToUpperInvariant()
        };
        variant.UpdateDetails(label, diameter, length, material, finish);
        return variant;
    }

    public void UpdateDetails(string label, decimal? diameter, decimal? length, string material, string finish)
    {
        ValidateDimension(diameter, "Diametru");
        ValidateDimension(length, "Lungime");
        var validLabel = Guard.Text(label, "Variantă", 160);
        var validMaterial = Guard.Text(material, "Material", 80);
        var validFinish = Guard.Text(finish, "Finisaj", 80);
        Label = validLabel;
        DiameterMm = diameter;
        LengthMm = length;
        Material = validMaterial;
        Finish = validFinish;
        SearchText = Product.Normalize(Sku + validLabel);
    }

    private static void ValidateDimension(decimal? value, string field)
    {
        if (value is not null && (value <= 0 || value > 999999999.999m || decimal.Round(value.Value, 3) != value))
            throw new BusinessException($"{field}: valoare pozitivă, maximum 999999999,999 mm și 3 zecimale.");
    }

    public void SetAttributes(IReadOnlyCollection<Guid> scope, IReadOnlyCollection<VariantAttributeValue> values)
    {
        if (values.Any(x => x.VariantId != Id || !scope.Contains(x.AttributeId)) || values.Select(x => x.AttributeId).Distinct().Count() != values.Count)
            throw new BusinessException("Atribute duplicate sau nepermise pentru această variantă.");
        AttributeValues.RemoveAll(x => scope.Contains(x.AttributeId) && values.All(v => v.AttributeId != x.AttributeId));
        foreach (var value in values)
        {
            var existing = AttributeValues.SingleOrDefault(x => x.AttributeId == value.AttributeId);
            if (existing is null) AttributeValues.Add(value);
            else existing.ReplaceWith(value);
        }
    }

    public SalesOffer AddOffer(string code, string label, int units, int min, int step)
    {
        var offer = SalesOffer.Create(Id, code, label, units, min, step);
        Offers.Add(offer);
        return offer;
    }
}
