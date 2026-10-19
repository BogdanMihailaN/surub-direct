namespace SurubDirect.Purchasing.Application;
public sealed record UpdateSupplierItem(string SupplierCode, string PackLabel, int UnitsPerPack, decimal NetCostPerPack, string Currency, bool IsActive);
