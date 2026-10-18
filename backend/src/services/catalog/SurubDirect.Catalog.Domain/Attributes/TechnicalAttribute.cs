using SurubDirect.BuildingBlocks;
using System.Text.RegularExpressions;

namespace SurubDirect.Catalog.Domain;
public sealed class TechnicalAttribute
{
    private TechnicalAttribute() { }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Code { get; private set; } = "";
    public string Name { get; private set; } = "";
    public AttributeKind Kind { get; private set; }
    public AttributeSource Source { get; private set; }
    public string? Unit { get; private set; }
    public int SortOrder { get; private set; }
    public static TechnicalAttribute Create(string code, string name, AttributeKind kind, string? unit, int sortOrder)
    {
        code = Guard.Text(code, "Cod atribut", 60);
        if (!Regex.IsMatch(code, "^[a-z][a-z0-9-]*$") || !Enum.IsDefined(kind))
            throw new BusinessException("Cod sau tip de atribut invalid.");
        if (kind == AttributeKind.Text && !string.IsNullOrWhiteSpace(unit))
            throw new BusinessException("Unitatea de măsură se aplică doar atributelor numerice.");
        var attribute = new TechnicalAttribute { Code = code, Kind = kind, Unit = string.IsNullOrWhiteSpace(unit) ? null : Guard.Text(unit, "Unitate", 20) };
        attribute.Update(name, sortOrder);
        return attribute;
    }
    public void Update(string name, int sortOrder)
    {
        var validName = Guard.Text(name, "Denumire atribut", 80);
        if (sortOrder < 0 || sortOrder > 10000) throw new BusinessException("Ordinea atributului nu este validă.");
        Name = validName;
        SortOrder = sortOrder;
    }
}
