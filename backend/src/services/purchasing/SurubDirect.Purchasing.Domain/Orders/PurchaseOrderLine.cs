using SurubDirect.BuildingBlocks;
namespace SurubDirect.Purchasing.Domain;
public sealed class PurchaseOrderLine
{
    private PurchaseOrderLine() { }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrderId { get; private set; }
    public Guid SupplierItemId { get; private set; }
    public Guid VariantId { get; private set; }
    public string Sku { get; private set; } = "";
    public string Label { get; private set; } = "";
    public string SupplierCode { get; private set; } = "";
    public string PackLabel { get; private set; } = "";
    public int UnitsPerPack { get; private set; }
    public int Packs { get; private set; }
    public decimal NetCostPerPack { get; private set; }
    public int BaseUnits => checked(Packs * UnitsPerPack);
    public decimal NetTotal => decimal.Round(Packs * NetCostPerPack, 2, MidpointRounding.AwayFromZero);
    public static PurchaseOrderLine Create(Guid orderId, SupplierItem item, int packs)
    {
        var line = new PurchaseOrderLine { OrderId = orderId, SupplierItemId = item.Id, VariantId = item.VariantId,
            Sku = item.Sku, Label = item.VariantLabel, SupplierCode = item.SupplierCode, PackLabel = item.PackLabel, UnitsPerPack = item.UnitsPerPack };
        line.Update(packs, item.NetCostPerPack); return line;
    }
    public void Update(int packs, decimal cost)
    {
        if (packs <= 0 || (long)packs * UnitsPerPack > int.MaxValue) throw new BusinessException("Cantitatea trebuie să fie pozitivă și să nu depășească 2.147.483.647 bucăți.");
        if (cost <= 0 || cost > 1000000 || decimal.Round(cost, 6) != cost) throw new BusinessException("Costul net: valoare pozitivă, maximum 1.000.000 și 6 zecimale.");
        if (packs * cost > 1000000000m) throw new BusinessException("Valoarea netă a unei linii nu poate depăși 1.000.000.000.");
        Packs = packs; NetCostPerPack = cost;
    }
}
