namespace SurubDirect.Purchasing.Application;
public sealed record UpdateSupplier(string Name, string CountryCode, string TaxId, string ContactName, string Email, string Phone, string Address, string Notes, bool IsActive);
