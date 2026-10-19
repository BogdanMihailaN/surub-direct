namespace SurubDirect.Purchasing.Contracts;
public sealed record SupplierPage(IReadOnlyList<SupplierDto> Items, int Total, int Page, int PageSize);
