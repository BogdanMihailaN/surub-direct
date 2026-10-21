using SurubDirect.BuildingBlocks;

namespace SurubDirect.Ordering.Domain;
public sealed record CustomerAddress(string Street, string City, string County, string PostalCode, string Country)
{
    public CustomerAddress Normalize()
    {
        var postal = CustomerText.Required(PostalCode, 6, "cod poștal");
        if (postal.Length != 6 || postal.Any(c => c < '0' || c > '9') || Country != "RO")
            throw new BusinessException("Adresa trebuie să fie în România, cu un cod poștal de 6 cifre.");
        return new(CustomerText.Required(Street, 200, "stradă și număr"), CustomerText.Required(City, 100, "localitate"), CustomerText.Required(County, 100, "județ"), postal, "RO");
    }
}
