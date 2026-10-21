namespace SurubDirect.Ordering.Domain;
public sealed class PreparedCart
{
    private PreparedCart()
    {
    }

    public Guid Id { get; private set; }
    public Guid CartId { get; private set; }
    public int SourceVersion { get; private set; }
    public CustomerDraft Customer { get; private set; } = null!;
    public List<PreparedCartLine> Lines { get; private set; } = [];
    public string Currency { get; private set; } = "";
    public DateTimeOffset CheckedAt { get; private set; }

    public static PreparedCart Create(Guid id, ShoppingCart cart, IReadOnlyList<PreparedCartLine> lines, string currency, DateTimeOffset checkedAt) => new()
    {
        Id = id,
        CartId = cart.Id,
        SourceVersion = cart.Version,
        Customer = cart.Customer!.Normalize(),
        Lines = lines.ToList(),
        Currency = currency,
        CheckedAt = checkedAt
    };
}
