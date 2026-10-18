namespace SurubDirect.Catalog.Application;
public record UpdateCategory(string Name, Guid? ParentId, int SortOrder, IReadOnlyList<Guid> AttributeIds);
