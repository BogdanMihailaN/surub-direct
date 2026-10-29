using SurubDirect.BuildingBlocks;

namespace SurubDirect.Ordering.Domain;
public sealed class ShoppingCart
{
    private ShoppingCart()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string? AccountId { get; private set; }
    public string OwnerHash { get; private set; } = "";
    public int Version { get; private set; } = 1;
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public List<CartSelection> Lines { get; private set; } = [];
    public Guid? PreparedId { get; private set; }
    public CustomerDraft? Customer { get; private set; }

    public static ShoppingCart Create(string ownerHash, DateTimeOffset now)
    {
        if (ownerHash.Length != 64)
            throw new BusinessException("Identitate coș invalidă.");
        return new()
        {
            OwnerHash = ownerHash,
            UpdatedAt = now,
            ExpiresAt = now.AddDays(30)
        };
    }

    public static ShoppingCart CreateForAccount(string accountId, string ownerHash, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(accountId) || accountId.Length > 450)
            throw new BusinessException("Cont invalid.");
        var cart = Create(ownerHash, now);
        cart.AccountId = accountId;
        return cart;
    }

    public void RestartExpired(DateTimeOffset now)
    {
        if (AccountId is null || ExpiresAt > now) return;
        if (PreparedId is not null)
            throw new BusinessException("Finalizează pregătirea înainte de a începe un coș nou.", 409);
        Lines = [];
        Customer = null;
        ExpiresAt = now.AddDays(30);
        UpdatedAt = now;
        Version = checked(Version + 1);
    }

    public void EnsureActive(DateTimeOffset now)
    {
        if (ExpiresAt <= now)
            throw new BusinessException("Coșul a expirat. Începe un coș nou.", 410);
    }

    public void Replace(Guid cartId, int version, IReadOnlyList<CartSelection>? lines, DateTimeOffset now)
    {
        EnsureActive(now);
        if (PreparedId is not null)
            throw new BusinessException("Anulează pregătirea înainte de a modifica acest coș.", 409);
        if (cartId != Id || version != Version)
            throw new BusinessException("Coșul s-a modificat. Reîncarcă înainte de a continua.", 409);
        if (lines is null || lines.Count > 50 || lines.Any(x => x is null) || lines.Select(x => x.OfferId).Distinct().Count() != lines.Count)
            throw new BusinessException("Coșul acceptă maximum 50 de ambalaje distincte.");
        foreach (var line in lines)
            line.Validate();
        Lines = lines.ToList();
        if (Lines.Count == 0)
            Customer = null;
        UpdatedAt = now;
        Version = checked(Version + 1);
    }

    public void SaveCustomer(Guid cartId, int version, CustomerDraft? customer, DateTimeOffset now)
    {
        EnsureActive(now);
        if (PreparedId is not null)
            throw new BusinessException("Anulează pregătirea înainte de a modifica acest coș.", 409);
        if (cartId != Id || version != Version)
            throw new BusinessException("Coșul s-a modificat. Reîncarcă înainte de a continua.", 409);
        if (customer is not null && Lines.Count == 0)
            throw new BusinessException("Adaugă produse înainte de a salva datele clientului.");
        var normalized = customer?.Normalize();
        Customer = normalized;
        UpdatedAt = now;
        Version = checked(Version + 1);
    }

    public void AttachPreparation(Guid id, int version, DateTimeOffset now)
    {
        EnsureActive(now);
        if (PreparedId is not null || Version != version || Customer is null || Lines.Count == 0)
            throw new BusinessException("Coșul s-a modificat sau nu este complet.", 409);
        PreparedId = id;
        Version = checked(Version + 1);
        UpdatedAt = now;
    }

    public void DetachPreparation(DateTimeOffset now)
    {
        PreparedId = null;
        Version = checked(Version + 1);
        UpdatedAt = now;
    }
}
