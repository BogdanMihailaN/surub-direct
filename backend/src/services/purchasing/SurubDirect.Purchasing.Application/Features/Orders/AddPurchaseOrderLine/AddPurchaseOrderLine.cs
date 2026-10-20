namespace SurubDirect.Purchasing.Application;
public sealed record AddPurchaseOrderLine(int Version, Guid SupplierItemId, int Packs);
