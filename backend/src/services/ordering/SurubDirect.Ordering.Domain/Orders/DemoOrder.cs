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
    public string? AccountId { get; private set; }
    public DateTimeOffset? AccountLinkedAt { get; private set; }
    public Guid CartId { get; private set; }
    public string Status { get; private set; } = "Confirming";
    public CustomerDraft Customer { get; private set; } = null!;
    public List<PreparedCartLine> Lines { get; private set; } = [];
    public string Currency { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; }
    public string? CancellationReason { get; private set; }
    public string? CancelledBy { get; private set; }
    public DateTimeOffset? CancellationRequestedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
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
    public void LinkAccount(string accountId, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(accountId) || accountId.Length > 450)
            throw new BusinessException("Identificatorul contului nu este valid.");
        if (AccountId is not null && AccountId != accountId)
            throw new BusinessException("Comanda nu este accesibilă.", 404);
        if (AccountId == accountId)
            return;
        if (Status is not ("Confirmed" or "Cancelled"))
            throw new BusinessException("Doar comenzile confirmate sau anulate pot fi asociate contului.", 409);
        AccountId = accountId;
        AccountLinkedAt = now;
    }

    public void Cancel(string reason, string actor, DateTimeOffset now)
    {
        reason = reason.Trim();
        if (reason.Length is < 3 or > 500 || string.IsNullOrWhiteSpace(actor) || actor.Length > 200)
            throw new BusinessException("Motivul anulării trebuie să aibă 3–500 caractere.");
        if (Status is "Cancelling" or "Cancelled")
        {
            if (CancellationReason != reason)
                throw new BusinessException("Anularea a fost deja cerută cu alt motiv.", 409);
            return;
        }

        if (Status != "Confirmed")
            throw new BusinessException("Doar o comandă confirmată poate fi anulată.", 409);
        CancellationReason = reason;
        CancelledBy = actor;
        CancellationRequestedAt = now;
        Status = "Cancelling";
    }

    public void Reconcile(ReservationPlan plan, DateTimeOffset now)
    {
        if (plan.Id != Id || !plan.ConfirmationRequested)
            throw new BusinessException("Planul nu confirmă această comandă.", 409);
        if (Status == "Cancelling" && plan.Status == "Restored")
        {
            Status = "Cancelled";
            CancelledAt = now;
            return;
        }

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
