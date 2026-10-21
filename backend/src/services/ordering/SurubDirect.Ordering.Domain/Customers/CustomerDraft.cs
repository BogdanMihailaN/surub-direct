using System.Net.Mail;
using System.Text.RegularExpressions;
using SurubDirect.BuildingBlocks;

namespace SurubDirect.Ordering.Domain;
public sealed record CustomerDraft(string CustomerType, string FullName, string Email, string Phone, string? CompanyName, string? TaxId, string? RegistrationNumber, CustomerAddress ShippingAddress, bool BillingSameAsShipping, CustomerAddress? BillingAddress)
{
    public CustomerDraft Normalize()
    {
        if (CustomerType is not ("person" or "company"))
            throw new BusinessException("Alege persoană fizică sau firmă.");
        var email = CustomerText.Required(Email, 254, "email");
        if (!MailAddress.TryCreate(email, out var parsed) || parsed.Address != email || !parsed.Host.Contains('.'))
            throw new BusinessException("Adresa de email nu este validă.");
        var phone = CustomerText.Required(Phone, 30, "telefon");
        if (!Regex.IsMatch(phone, @"^\+?[0-9 ()-]+$") || phone.Count(char.IsAsciiDigit)is < 7 or > 15)
            throw new BusinessException("Numărul de telefon nu este valid.");
        string? company = null, tax = null, registration = null;
        if (CustomerType == "company")
        {
            company = CustomerText.Required(CompanyName, 200, "denumire firmă");
            tax = CustomerText.Required(TaxId, 12, "CUI").ToUpperInvariant();
            if (!Regex.IsMatch(tax, @"^(RO)?[0-9]{2,10}$"))
                throw new BusinessException("CUI trebuie să conțină 2–10 cifre, opțional precedate de RO.");
            registration = CustomerText.Optional(RegistrationNumber, 40, "număr registrul comerțului");
        }

        if (ShippingAddress is null || (!BillingSameAsShipping && BillingAddress is null))
            throw new BusinessException("Completează adresele de livrare și facturare.");
        return new(CustomerType, CustomerText.Required(FullName, 150, "nume complet"), email, phone, company, tax, registration, ShippingAddress.Normalize(), BillingSameAsShipping, BillingSameAsShipping ? null : BillingAddress!.Normalize());
    }
}
