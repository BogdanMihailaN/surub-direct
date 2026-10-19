using SurubDirect.Purchasing.Contracts;
using SurubDirect.Purchasing.Domain;
namespace SurubDirect.Purchasing.Infrastructure;
internal static class SupplierProjection
{
    public static SupplierDto Map(Supplier s) => new(s.Id, s.Name, s.CountryCode, s.TaxId, s.ContactName, s.Email, s.Phone, s.Address, s.Notes, s.IsActive,
        s.Items.OrderBy(x => x.Sku).Select(x => new SupplierItemDto(x.Id, x.VariantId, x.Sku, x.VariantLabel, x.SupplierCode, x.PackLabel, x.UnitsPerPack, x.NetCostPerPack, x.Currency, x.IsActive, x.UpdatedAt)).ToArray());
}
