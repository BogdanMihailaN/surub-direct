namespace SurubDirect.Store.Application;
public record StorePage(IReadOnlyList<StoreProduct> Items, int Total, int Page, int PageSize);
