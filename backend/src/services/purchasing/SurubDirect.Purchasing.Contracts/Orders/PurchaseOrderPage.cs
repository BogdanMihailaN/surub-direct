namespace SurubDirect.Purchasing.Contracts;
public sealed record PurchaseOrderPage(IReadOnlyList<PurchaseOrderDto> Items, int Total, int Page, int PageSize);
