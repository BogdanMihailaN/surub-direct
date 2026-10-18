namespace SurubDirect.Catalog.Domain;
public sealed class CategoryAttribute(Guid categoryId, Guid attributeId)
{
    public Guid CategoryId { get; private set; } = categoryId;
    public Guid AttributeId { get; private set; } = attributeId;
}
