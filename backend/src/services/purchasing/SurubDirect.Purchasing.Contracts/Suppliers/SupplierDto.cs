namespace SurubDirect.Purchasing.Contracts;
public sealed record SupplierDto(Guid Id, string Name, string CountryCode, string TaxId, string ContactName, string Email, string Phone, string Address, string Notes, bool IsActive, IReadOnlyList<SupplierItemDto> Items);
