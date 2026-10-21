using SurubDirect.Ordering.Domain;
using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Application;
internal static class CustomerDraftProjection
{
    public static CustomerDraftDto? Map(CustomerDraft? c) => c is null ? null : new(c.CustomerType, c.FullName, c.Email, c.Phone, c.CompanyName, c.TaxId, c.RegistrationNumber, Address(c.ShippingAddress), c.BillingSameAsShipping, c.BillingAddress is null ? null : Address(c.BillingAddress));
    private static CustomerAddressDto Address(CustomerAddress a) => new(a.Street, a.City, a.County, a.PostalCode, a.Country);
}
