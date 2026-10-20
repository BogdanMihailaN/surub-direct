using SurubDirect.Purchasing.Domain;
using SurubDirect.Purchasing.Contracts;
namespace SurubDirect.Purchasing.Infrastructure;

internal static class PurchaseOrderProjection
{
    public static PurchaseOrderDto Map(PurchaseOrder o) => new(o.Id, o.Number, o.SupplierId, o.SupplierName, o.SupplierCountry, o.SupplierTaxId, o.SupplierEmail, o.SupplierAddress,
        o.Currency, o.Notes, o.Status.ToString(), o.Version, o.CreatedAt, o.SentAt, o.CancelledAt, o.CancellationReason, o.NetTotal,
        o.Lines.OrderBy(l => l.Sku).Select(l => new PurchaseOrderLineDto(l.Id, l.SupplierItemId, l.VariantId, l.Sku, l.Label, l.SupplierCode, l.PackLabel, l.UnitsPerPack, l.Packs, l.BaseUnits, l.NetCostPerPack, l.NetTotal)).ToArray(),
        o.Receipts.OrderBy(r => r.CreatedAt).ThenBy(r => r.Id).Select(r => new PurchaseReceiptDto(r.Id, r.LineId, r.VariantId, r.Quantity, r.Reference, r.Actor, r.CreatedAt, r.AppliedAt)).ToArray(),
        o.Corrections.OrderBy(c => c.CreatedAt).ThenBy(c => c.Id).Select(c => new PurchaseReceiptCorrectionDto(c.Id, c.ReceiptId, c.LineId, c.Quantity, c.Reference, c.Reason, c.Actor, c.CreatedAt, c.AppliedAt)).ToArray());
}
