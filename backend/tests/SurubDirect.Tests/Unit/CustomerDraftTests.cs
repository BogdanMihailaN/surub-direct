using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Domain;
using Xunit;

namespace SurubDirect.Tests.Unit;
public sealed class CustomerDraftTests
{
    [Fact]
    public void Normalizes_company_and_discards_irrelevant_person_fields()
    {
        var input = CustomerDraftSamples.Person()with
        {
            CustomerType = "company",
            FullName = " Client Demo ",
            CompanyName = " Firma Demo ",
            TaxId = "ro123456",
            RegistrationNumber = " J00/1/2020 ",
            BillingSameAsShipping = false,
            BillingAddress = new("Facturare 2", "Cluj-Napoca", "Cluj", "400001", "RO")
        };
        var company = input.Normalize();
        Assert.Equal("Client Demo", company.FullName);
        Assert.Equal("RO123456", company.TaxId);
        Assert.Equal("Firma Demo", company.CompanyName);
        Assert.NotNull(company.BillingAddress);
        var person = (company with
        {
            CustomerType = "person",
            BillingSameAsShipping = true
        }

        ).Normalize();
        Assert.Null(person.CompanyName);
        Assert.Null(person.TaxId);
        Assert.Null(person.RegistrationNumber);
        Assert.Null(person.BillingAddress);
    }

    [Theory]
    [InlineData("email")]
    [InlineData("phone")]
    [InlineData("company")]
    [InlineData("tax")]
    [InlineData("shipping")]
    [InlineData("billing")]
    [InlineData("postal")]
    [InlineData("country")]
    [InlineData("name")]
    [InlineData("type")]
    public void Rejects_invalid_customer_fields(string field)
    {
        var value = CustomerDraftSamples.Person();
        value = field switch
        {
            "email" => value with
            {
                Email = "not-email"
            },
            "phone" => value with
            {
                Phone = "123"
            },
            "company" => value with
            {
                CustomerType = "company",
                TaxId = "123456",
                CompanyName = " "
            },
            "tax" => value with
            {
                CustomerType = "company",
                TaxId = "invalid",
                CompanyName = "Demo"
            },
            "shipping" => value with
            {
                ShippingAddress = null!
            },
            "billing" => value with
            {
                BillingSameAsShipping = false
            },
            "postal" => value with
            {
                ShippingAddress = value.ShippingAddress with
                {
                    PostalCode = "12a456"
                }
            },
            "country" => value with
            {
                ShippingAddress = value.ShippingAddress with
                {
                    Country = "XX"
                }
            },
            "name" => value with
            {
                FullName = "Client\nInjected"
            },
            _ => value with
            {
                CustomerType = "unknown"
            }
        };
        Assert.Throws<BusinessException>(() => value.Normalize());
    }

    [Fact]
    public void Customer_draft_requires_items_and_current_unexpired_version()
    {
        var now = DateTimeOffset.UtcNow;
        var cart = ShoppingCart.Create(new string ('a', 64), now);
        Assert.Throws<BusinessException>(() => cart.SaveCustomer(cart.Id, 1, CustomerDraftSamples.Person(), now));
        cart.Replace(cart.Id, 1, [new("surub", Guid.NewGuid(), 1)], now);
        Assert.Equal(409, Assert.Throws<BusinessException>(() => cart.SaveCustomer(cart.Id, 1, CustomerDraftSamples.Person(), now)).Status);
        Assert.Equal(410, Assert.Throws<BusinessException>(() => cart.SaveCustomer(cart.Id, 2, CustomerDraftSamples.Person(), now.AddDays(30))).Status);
        Assert.Null(cart.Customer);
        Assert.Equal(2, cart.Version);
    }
}
