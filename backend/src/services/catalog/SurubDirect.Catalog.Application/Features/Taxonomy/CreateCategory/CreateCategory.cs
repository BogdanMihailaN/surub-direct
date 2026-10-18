namespace SurubDirect.Catalog.Application;
public record CreateCategory(string Name, string Slug, Guid? ParentId, int SortOrder, IReadOnlyList<Guid> AttributeIds);
