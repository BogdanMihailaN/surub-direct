namespace SurubDirect.Purchasing.Application;
public sealed record CreatePurchaseOrder(Guid SupplierId, string Currency, string? Notes);
