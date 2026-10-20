using SurubDirect.BuildingBlocks;
namespace SurubDirect.Purchasing.Domain;
public sealed class PurchaseOrder
{
    private PurchaseOrder() { }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public long Number { get; private set; }
    public Guid SupplierId { get; private set; }
    public string SupplierName { get; private set; } = "";
    public string SupplierCountry { get; private set; } = "";
    public string SupplierTaxId { get; private set; } = "";
    public string SupplierEmail { get; private set; } = "";
    public string SupplierAddress { get; private set; } = "";
    public string Currency { get; private set; } = "";
    public string Notes { get; private set; } = "";
    public PurchaseOrderStatus Status { get; private set; }
    public int Version { get; private set; } = 1;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SentAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public string CancellationReason { get; private set; } = "";
    public List<PurchaseOrderLine> Lines { get; private set; } = [];
    public List<PurchaseReceipt> Receipts { get; private set; } = [];
    public decimal NetTotal => Lines.Sum(x => x.NetTotal);
    public static PurchaseOrder Create(Supplier supplier, string currency, string? notes)
    {
        if (!supplier.IsActive) throw new BusinessException("Furnizorul este inactiv.", 409);
        currency = Guard.Text(currency, "Monedă", 3).ToUpperInvariant();
        if (!new[] { "RON", "EUR", "USD", "GBP", "CHF" }.Contains(currency)) throw new BusinessException("Monedă neacceptată.");
        return new() { SupplierId = supplier.Id, SupplierName = supplier.Name, SupplierCountry = supplier.CountryCode,
            SupplierTaxId = supplier.TaxId, SupplierEmail = supplier.Email, SupplierAddress = supplier.Address, Currency = currency, Notes = ValidNotes(notes) };
    }
    public void EnsureVersion(int expected)
    {
        if (Version != expected) throw new BusinessException("Comanda a fost modificată. Reîncarcă datele înainte de a continua.", 409);
    }
    public void UpdateNotes(string? notes) { EnsureDraft(); Notes = ValidNotes(notes); Version++; }
    public PurchaseOrderLine AddLine(Supplier supplier, Guid itemId, int packs)
    {
        EnsureDraft(); EnsureSupplier(supplier);
        var item = supplier.Items.SingleOrDefault(x => x.Id == itemId && x.IsActive) ?? throw new BusinessException("Asocierea furnizor–SKU nu există sau este inactivă.", 409);
        if (item.Currency != Currency) throw new BusinessException("Articolul trebuie să folosească moneda comenzii.");
        if (Lines.Count >= 50) throw new BusinessException("Maximum 50 de articole per comandă.");
        if (Lines.Any(x => x.SupplierItemId == itemId)) throw new BusinessException("Articolul există deja. Editează cantitatea liniei existente.", 409);
        var line = PurchaseOrderLine.Create(Id, item, packs); Lines.Add(line); Version++; return line;
    }
    public void UpdateLine(Guid id, int packs, decimal cost) { EnsureDraft(); Line(id).Update(packs, cost); Version++; }
    public void RemoveLine(Guid id) { EnsureDraft(); Lines.Remove(Line(id)); Version++; }
    public void EnsureCanSend(Supplier supplier)
    {
        EnsureDraft(); EnsureSupplier(supplier);
        if (Lines.Count == 0) throw new BusinessException("Adaugă cel puțin un articol înainte de marcarea trimiterii.");
        if (Lines.Any(l => !supplier.Items.Any(i => i.Id == l.SupplierItemId && i.IsActive && i.VariantId == l.VariantId)))
            throw new BusinessException("Comanda conține asocieri inactive. Verifică articolele furnizorului.", 409);
    }
    public void MarkSent(Supplier supplier) { EnsureCanSend(supplier); Status = PurchaseOrderStatus.Sent; SentAt = DateTimeOffset.UtcNow; Version++; }
    public void Cancel(string reason)
    {
        if (Receipts.Count > 0) throw new BusinessException("Comanda are recepții și nu poate fi anulată.", 409);
        if (Status == PurchaseOrderStatus.Cancelled) throw new BusinessException("Comanda este deja anulată.", 409);
        CancellationReason = Guard.Text(reason, "Motiv anulare", 500); Status = PurchaseOrderStatus.Cancelled; CancelledAt = DateTimeOffset.UtcNow; Version++;
    }
    public PurchaseReceipt Receive(Guid requestId, Guid lineId, int quantity, string reference, string actor)
    {
        if (Status != PurchaseOrderStatus.Sent) throw new BusinessException("Se pot recepționa doar comenzi trimise.", 409);
        var line = Line(lineId);
        var receipt = PurchaseReceipt.Create(requestId, Id, line, quantity, reference, actor);
        if (Receipts.Any(r => r.LineId == lineId && r.Reference == receipt.Reference))
            throw new BusinessException("Documentul există deja pentru această linie. Reia recepția existentă.", 409);
        if (Receipts.Where(r => r.LineId == lineId).Sum(r => (long)r.Quantity) + quantity > line.BaseUnits)
            throw new BusinessException("Cantitatea depășește restul de recepționat, inclusiv recepțiile în așteptare.", 409);
        Receipts.Add(receipt); Version++; return receipt;
    }
    public void CompleteReceipt(Guid id)
    {
        var receipt = Receipts.Single(r => r.Id == id);
        if (receipt.AppliedAt is null) { receipt.MarkApplied(); Version++; }
    }
    private void EnsureDraft() { if (Status != PurchaseOrderStatus.Draft) throw new BusinessException("Doar comenzile în ciornă pot fi editate.", 409); }
    private void EnsureSupplier(Supplier supplier) { if (supplier.Id != SupplierId || !supplier.IsActive) throw new BusinessException("Furnizorul comenzii este inactiv sau invalid.", 409); }
    private PurchaseOrderLine Line(Guid id) => Lines.SingleOrDefault(x => x.Id == id) ?? throw new BusinessException("Linia nu există în această comandă.", 404);
    private static string ValidNotes(string? notes) { notes = notes?.Trim() ?? ""; if (notes.Length > 2000) throw new BusinessException("Observații: maximum 2000 caractere."); return notes; }
}
