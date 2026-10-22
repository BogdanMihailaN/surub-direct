using SurubDirect.BuildingBlocks;

namespace SurubDirect.Ordering.Domain;
public sealed class DemoOrder
{
    private DemoOrder()
    {
    }

    public Guid Id { get; private set; }
    public long Sequence { get; private set; }
    public string OwnerHash { get; private set; } = "";
    public Guid CartId { get; private set; }
    public string Status { get; private set; } = "Confirming";
    public CustomerDraft Customer { get; private set; } = null!;
    public List<PreparedCartLine> Lines { get; private set; } = [];
    public string Currency { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? FinalizedAt { get; private set; }

    public static DemoOrder Create(string owner, PreparedCart snapshot, DateTimeOffset now) => new()
    {
        Id = snapshot.Id,
        OwnerHash = owner,
        CartId = snapshot.CartId,
        Customer = snapshot.Customer.Normalize(),
        Lines = snapshot.Lines.ToList(),
        Currency = snapshot.Currency,
        CreatedAt = now
    };
    public void Reconcile(ReservationPlan plan, DateTimeOffset now)
    {
        if (plan.Id != Id || !plan.ConfirmationRequested)
            throw new BusinessException("Planul nu confirmă această comandă.", 409);
        if (Status != "Confirming")
            return;
        if (plan.Status == "Consumed")
            Status = "Confirmed";
        else if (plan.Status is "Expired" or "Released")
            Status = "Failed";
        else
            return;
        FinalizedAt = now;
    }
}
