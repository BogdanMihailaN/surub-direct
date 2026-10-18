namespace SurubDirect.Store.Application;
public record StockSummary(Guid VariantId, long OnHand, long Reserved, long Available);
