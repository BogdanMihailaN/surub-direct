namespace SurubDirect.Inventory.Contracts;
public record StockDto(Guid VariantId, long OnHand, long Reserved, long Available);
