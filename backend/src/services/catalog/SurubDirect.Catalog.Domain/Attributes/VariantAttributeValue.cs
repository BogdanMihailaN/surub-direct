using SurubDirect.BuildingBlocks;

namespace SurubDirect.Catalog.Domain;
public sealed class VariantAttributeValue
{
    private VariantAttributeValue() { }
    public Guid VariantId { get; private set; }
    public Guid AttributeId { get; private set; }
    public string? TextValue { get; private set; }
    public string? NormalizedText { get; private set; }
    public decimal? NumberValue { get; private set; }
    public static VariantAttributeValue Create(Guid variantId, TechnicalAttribute definition, string? text, decimal? number)
    {
        if (definition.Source != AttributeSource.Custom)
            throw new BusinessException("Atributul utilizează câmpul existent al variantei.");
        if (definition.Kind == AttributeKind.Text)
        {
            if (number is not null) throw new BusinessException("Atributul acceptă doar text.");
            text = Guard.Text(text, definition.Name, 120);
        }
        else if (text is not null || number is null || (number.Value < -999999999999.999999m || number.Value > 999999999999.999999m) || decimal.Round(number.Value, 6) != number)
            throw new BusinessException("Atributul numeric acceptă maximum 6 zecimale și 12 cifre întregi.");
        return new() { VariantId = variantId, AttributeId = definition.Id, TextValue = text, NormalizedText = text?.ToUpperInvariant(), NumberValue = number };
    }
    public void ReplaceWith(VariantAttributeValue value)
    {
        TextValue = value.TextValue;
        NormalizedText = value.NormalizedText;
        NumberValue = value.NumberValue;
    }
}
