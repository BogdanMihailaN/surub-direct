namespace SurubDirect.Purchasing.Application;
public sealed record AddSupplierItem(Guid VariantId, string SupplierCode, string PackLabel, int UnitsPerPack, decimal NetCostPerPack, string Currency);
