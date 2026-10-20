namespace SurubDirect.Inventory.Contracts;
public sealed record StockLedgerDto(long OnHand, long Reserved, IReadOnlyList<StockMovementDto> Items, int Total, int Page, int PageSize);
