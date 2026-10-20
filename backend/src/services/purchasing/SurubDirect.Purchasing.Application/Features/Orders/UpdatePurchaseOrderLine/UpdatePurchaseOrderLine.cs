namespace SurubDirect.Purchasing.Application;
public sealed record UpdatePurchaseOrderLine(int Version, int Packs, decimal NetCostPerPack);
