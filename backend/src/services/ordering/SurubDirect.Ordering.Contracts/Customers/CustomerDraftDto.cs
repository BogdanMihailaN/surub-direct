namespace SurubDirect.Ordering.Contracts;
public sealed record CustomerDraftDto(string CustomerType, string FullName, string Email, string Phone, string? CompanyName, string? TaxId, string? RegistrationNumber, CustomerAddressDto ShippingAddress, bool BillingSameAsShipping, CustomerAddressDto? BillingAddress);
