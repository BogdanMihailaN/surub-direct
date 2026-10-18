namespace SurubDirect.Catalog.Contracts;
public record CategoryDto(Guid Id, string Name, string Slug, Guid? ParentId, int SortOrder, IReadOnlyList<Guid> AttributeIds);
