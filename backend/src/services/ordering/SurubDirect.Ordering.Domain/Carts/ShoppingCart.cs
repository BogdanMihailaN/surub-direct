using SurubDirect.BuildingBlocks;

namespace SurubDirect.Ordering.Domain;
public sealed class ShoppingCart
{
    private ShoppingCart()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string OwnerHash { get; private set; } = "";
    public int Version { get; private set; } = 1;
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public List<CartSelection> Lines { get; private set; } = [];

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

    public void EnsureActive(DateTimeOffset now)
    {
        if (ExpiresAt <= now)
            throw new BusinessException("Coșul a expirat. Începe un coș nou.", 410);
    }

    public void Replace(Guid cartId, int version, IReadOnlyList<CartSelection>? lines, DateTimeOffset now)
    {
        EnsureActive(now);
        if (cartId != Id || version != Version)
            throw new BusinessException("Coșul s-a modificat. Reîncarcă înainte de a continua.", 409);
        if (lines is null || lines.Count > 50 || lines.Any(x => x is null) || lines.Select(x => x.OfferId).Distinct().Count() != lines.Count)
            throw new BusinessException("Coșul acceptă maximum 50 de ambalaje distincte.");
        foreach (var line in lines)
            line.Validate();
        Lines = lines.ToList();
        UpdatedAt = now;
        Version = checked(Version + 1);
    }
}
