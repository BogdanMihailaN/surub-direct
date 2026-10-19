using System.Net.Mail;
using System.Text.RegularExpressions;
using SurubDirect.BuildingBlocks;
namespace SurubDirect.Purchasing.Domain;
public sealed class Supplier
{
    private Supplier() { }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = "";
    public string CountryCode { get; private set; } = "";
    public string TaxId { get; private set; } = "";
    public string ContactName { get; private set; } = "";
    public string Email { get; private set; } = "";
    public string Phone { get; private set; } = "";
    public string Address { get; private set; } = "";
    public string Notes { get; private set; } = "";
    public bool IsActive { get; private set; } = true;
    public List<SupplierItem> Items { get; private set; } = [];
    public static Supplier Create(string name, string countryCode, string taxId, string contactName, string email, string phone, string address, string notes)
    {
        var supplier = new Supplier();
        supplier.Update(name, countryCode, taxId, contactName, email, phone, address, notes, true);
        return supplier;
    }
    public void Update(string name, string countryCode, string taxId, string contactName, string email, string phone, string address, string notes, bool active)
    {
        name = Guard.Text(name, "Denumire furnizor");
        countryCode = Guard.Text(countryCode, "Țară", 2).ToUpperInvariant();
        if (!Regex.IsMatch(countryCode, "^[A-Z]{2}$")) throw new BusinessException("Țara trebuie exprimată prin două litere, de exemplu RO.");
        taxId = string.Concat(Guard.Text(taxId, "Cod fiscal", 40).Where(c => !char.IsWhiteSpace(c))).ToUpperInvariant();
        if (countryCode == "RO" && taxId.StartsWith("RO", StringComparison.Ordinal)) taxId = taxId[2..];
        if (!Regex.IsMatch(taxId, "^[A-Z0-9-]{1,40}$")) throw new BusinessException("Cod fiscal invalid: folosește litere, cifre sau cratimă.");
        contactName = Optional(contactName, "Persoană de contact", 160);
        email = Optional(email, "Email", 254);
        if (email.Length > 0 && (!MailAddress.TryCreate(email, out var parsed) || parsed.Address != email)) throw new BusinessException("Adresa de email nu este validă.");
        phone = Optional(phone, "Telefon", 40);
        address = Optional(address, "Adresă", 500);
        notes = Optional(notes, "Observații", 2000);
        Name = name; CountryCode = countryCode; TaxId = taxId; ContactName = contactName;
        Email = email; Phone = phone; Address = address; Notes = notes; IsActive = active;
    }
    public SupplierItem AddItem(Guid variantId, string sku, string label, string supplierCode, string packLabel, int units, decimal cost, string currency)
    {
        if (!IsActive) throw new BusinessException("Reactivează furnizorul înainte de a adăuga articole.", 409);
        if (Items.Any(x => x.VariantId == variantId)) throw new BusinessException("SKU-ul este deja asociat furnizorului. Editează asocierea existentă.", 409);
        var item = SupplierItem.Create(Id, variantId, sku, label, supplierCode, packLabel, units, cost, currency);
        EnsureUniqueCode(item.SupplierCode, item.Id);
        Items.Add(item); return item;
    }
    public void UpdateItem(Guid id, string code, string packLabel, int units, decimal cost, string currency, bool active)
    {
        if (!IsActive) throw new BusinessException("Reactivează furnizorul înainte de a modifica articolele.", 409);
        var item = Items.SingleOrDefault(x => x.Id == id) ?? throw new BusinessException("Articolul nu există la acest furnizor.", 404);
        EnsureUniqueCode(Guard.Text(code, "Cod furnizor", 80).ToUpperInvariant(), id);
        item.Update(code, packLabel, units, cost, currency, active);
    }
    private void EnsureUniqueCode(string code, Guid id)
    {
        if (Items.Any(x => x.SupplierCode == code && x.Id != id)) throw new BusinessException("Codul articolului există deja la acest furnizor.", 409);
    }
    private static string Optional(string? value, string name, int limit)
    {
        value = value?.Trim() ?? "";
        if (value.Length > limit) throw new BusinessException($"{name}: maximum {limit} caractere.");
        return value;
    }
}
