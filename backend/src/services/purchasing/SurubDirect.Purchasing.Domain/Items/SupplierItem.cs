using SurubDirect.BuildingBlocks;
namespace SurubDirect.Purchasing.Domain;
public sealed class SupplierItem
{
    private SupplierItem() { }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid SupplierId { get; private set; }
    public Guid VariantId { get; private set; }
    public string Sku { get; private set; } = "";
    public string VariantLabel { get; private set; } = "";
    public string SupplierCode { get; private set; } = "";
    public string PackLabel { get; private set; } = "";
    public int UnitsPerPack { get; private set; }
    public decimal NetCostPerPack { get; private set; }
    public string Currency { get; private set; } = "";
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset UpdatedAt { get; private set; }
    public static SupplierItem Create(Guid supplierId, Guid variantId, string sku, string label, string code, string packLabel, int units, decimal cost, string currency)
    {
        if (supplierId == Guid.Empty || variantId == Guid.Empty) throw new BusinessException("Furnizorul și varianta sunt obligatorii.");
        var item = new SupplierItem { SupplierId = supplierId, VariantId = variantId, Sku = Guard.Text(sku, "SKU", 80), VariantLabel = Guard.Text(label, "Variantă", 160) };
        item.Update(code, packLabel, units, cost, currency, true); return item;
    }
    public void Update(string code, string packLabel, int units, decimal cost, string currency, bool active)
    {
        code = Guard.Text(code, "Cod furnizor", 80).ToUpperInvariant();
        packLabel = Guard.Text(packLabel, "Ambalaj de achiziție", 100);
        Guard.Positive(units, "Bucăți per ambalaj");
        if (cost <= 0 || cost > 1000000 || decimal.Round(cost, 6) != cost) throw new BusinessException("Costul net trebuie să fie pozitiv, maximum 1.000.000 și 6 zecimale.");
        currency = Guard.Text(currency, "Monedă", 3).ToUpperInvariant();
        if (!new[] { "RON", "EUR", "USD", "GBP", "CHF" }.Contains(currency)) throw new BusinessException("Monede acceptate: RON, EUR, USD, GBP, CHF.");
        SupplierCode = code; PackLabel = packLabel; UnitsPerPack = units; NetCostPerPack = cost;
        Currency = currency; IsActive = active; UpdatedAt = DateTimeOffset.UtcNow;
    }
}
