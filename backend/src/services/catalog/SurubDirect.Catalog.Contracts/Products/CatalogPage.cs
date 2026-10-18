namespace SurubDirect.Catalog.Contracts;
public record CatalogPage(IReadOnlyList<ProductDto> Items, int Total, int Page, int PageSize);
