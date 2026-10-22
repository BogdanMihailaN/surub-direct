namespace SurubDirect.Ordering.Contracts;
public sealed record AdminOrderPageDto(IReadOnlyList<AdminOrderSummaryDto> Items, int Total, int Page, int PageSize);
