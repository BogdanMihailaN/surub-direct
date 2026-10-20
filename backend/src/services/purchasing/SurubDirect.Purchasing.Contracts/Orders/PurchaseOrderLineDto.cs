namespace SurubDirect.Purchasing.Contracts;
public sealed record PurchaseOrderLineDto(Guid Id, Guid SupplierItemId, Guid VariantId, string Sku, string Label, string SupplierCode, string PackLabel, int UnitsPerPack, int Packs, int BaseUnits, decimal NetCostPerPack, decimal NetTotal);
