namespace SurubDirect.Catalog.Contracts;
public record CatalogFilter(string? Search = null, string? Category = null, decimal? Diameter = null, decimal? Length = null, int Page = 1, int PageSize = 12, string? Status = null, Guid? CategoryId = null, IReadOnlyList<AttributeFilter>? Attributes = null);
