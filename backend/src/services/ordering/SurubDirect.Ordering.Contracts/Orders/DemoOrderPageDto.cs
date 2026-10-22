namespace SurubDirect.Ordering.Contracts;
public sealed record DemoOrderPageDto(IReadOnlyList<DemoOrderDto> Items, int Page, int Total, int PageSize);
