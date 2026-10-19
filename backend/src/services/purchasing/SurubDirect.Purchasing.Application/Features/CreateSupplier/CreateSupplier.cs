namespace SurubDirect.Purchasing.Application;
public sealed record CreateSupplier(string Name, string CountryCode, string TaxId, string ContactName, string Email, string Phone, string Address, string Notes);
