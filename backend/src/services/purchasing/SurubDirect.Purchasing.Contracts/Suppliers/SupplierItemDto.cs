namespace SurubDirect.Purchasing.Contracts;
public sealed record SupplierItemDto(Guid Id, Guid VariantId, string Sku, string VariantLabel, string SupplierCode, string PackLabel, int UnitsPerPack, decimal NetCostPerPack, string Currency, bool IsActive, DateTimeOffset UpdatedAt);
