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
    public string ProcessingStatus { get; private set; } = "Pending";
    public long ProcessingVersion { get; private set; }
    public string ShipmentStatus { get; private set; } = "NotStarted";
    public string? ShipmentReference { get; private set; }
    public long ShipmentVersion { get; private set; }
    public CustomerDraft Customer { get; private set; } = null!;
    public List<PreparedCartLine> Lines { get; private set; } = [];
    public DeliveryCharge? Delivery { get; private set; }
    public string Currency { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; }
    public string? CancellationReason { get; private set; }
    public string? CancelledBy { get; private set; }
    public DateTimeOffset? CancellationRequestedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public DateTimeOffset? FinalizedAt { get; private set; }

    public static DemoOrder Create(string owner, PreparedCart snapshot, DateTimeOffset now, string? accountId = null)
    {
        if (accountId is not null && (string.IsNullOrWhiteSpace(accountId) || accountId.Length > 450))
            throw new BusinessException("Identificatorul contului nu este valid.");
        return new()
        {
            Id = snapshot.Id,
            OwnerHash = owner,
            AccountId = accountId,
            AccountLinkedAt = accountId is null ? null : now,
            CartId = snapshot.CartId,
            Customer = snapshot.Customer.Normalize(),
            Lines = snapshot.Lines.ToList(),
            Currency = snapshot.Currency,
            Delivery = snapshot.Delivery,
            CreatedAt = now
        };
    }
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

    public OrderProcessingChange ChangeProcessing(Guid operationId, long expectedVersion, string target, string reason, string actor, DateTimeOffset now)
    {
        reason = reason.Trim();
        if (operationId == Guid.Empty || expectedVersion < 0 || reason.Length is < 3 or > 500
            || string.IsNullOrWhiteSpace(actor) || actor.Length > 200)
            throw new BusinessException("Operația și un motiv de 3–500 caractere sunt obligatorii.");
        if (Status != "Confirmed")
            throw new BusinessException("Doar comenzile confirmate pot fi pregătite.", 409);
        if (ShipmentStatus != "NotStarted")
            throw new BusinessException("Pregătirea este închisă după crearea expedierii demonstrative.", 409);
        if (expectedVersion != ProcessingVersion)
            throw new BusinessException("Pregătirea comenzii s-a modificat. Actualizează detaliile.", 409);
        if (ProcessingVersion >= 1000)
            throw new BusinessException("Limita schimbărilor demonstrative a fost atinsă.", 409);
        if (!((ProcessingStatus == "Pending" && target == "Processing")
            || (ProcessingStatus == "Processing" && target == "Ready")
            || (ProcessingStatus == "Ready" && target == "Processing")))
            throw new BusinessException("Tranziția de pregătire nu este permisă.", 409);
        var change = new OrderProcessingChange(Id, operationId, ++ProcessingVersion, ProcessingStatus, target, reason, actor, now);
        ProcessingStatus = target;
        return change;
    }

    public ShipmentChange ChangeShipment(Guid operationId, long expectedVersion, string target, string reason, string actor, string? paymentStatus, DateTimeOffset now)
    {
        reason = reason.Trim();
        if (operationId == Guid.Empty || expectedVersion < 0 || reason.Length is < 3 or > 500
            || string.IsNullOrWhiteSpace(actor) || actor.Length > 200)
            throw new BusinessException("Operația și un motiv de 3–500 caractere sunt obligatorii.");
        if (Status != "Confirmed" || ProcessingStatus != "Ready" || Delivery is null)
            throw new BusinessException("Expedierea necesită o comandă confirmată, pregătită și cu transport selectat.", 409);
        if (expectedVersion != ShipmentVersion)
            throw new BusinessException("Expedierea s-a modificat. Actualizează comanda.", 409);
        if (!((ShipmentStatus == "NotStarted" && target == "Ready")
            || (ShipmentStatus == "Ready" && target == "Shipped")
            || (ShipmentStatus == "Shipped" && target == "Delivered")))
            throw new BusinessException("Tranziția de expediere nu este permisă.", 409);
        if (target == "Shipped" && paymentStatus != "Paid")
            throw new BusinessException("Expedierea demonstrativă necesită o plată simulată reușită.", 409);
        var change = new ShipmentChange(Id, operationId, ++ShipmentVersion, ShipmentStatus, target, reason, actor, now);
        ShipmentStatus = target;
        ShipmentReference ??= $"DEMO-{Id:N}".ToUpperInvariant();
        return change;
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
        if (ShipmentStatus is "Shipped" or "Delivered")
            throw new BusinessException("Comanda expediată nu poate fi anulată cu restituire automată de stoc. Este necesar un flux de retur.", 409);
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
