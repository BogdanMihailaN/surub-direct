using SurubDirect.BuildingBlocks;

namespace SurubDirect.Ordering.Domain;
public sealed class ReservationPlan
{
    private ReservationPlan()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public List<ReservationItem> Lines { get; private set; } = [];
    public string Status { get; private set; } = "Pending";
    public bool CancellationRequested { get; private set; }
    public string? LastError { get; private set; }
    public int Attempts { get; private set; }
    public DateTimeOffset Deadline { get; private set; }
    public DateTimeOffset NextAttemptAt { get; private set; }
    public DateTimeOffset? RemoteExpiresAt { get; private set; }
    public bool Terminal => Status is "Released" or "Expired";

    public static ReservationPlan Create(Guid id, Guid order, IReadOnlyList<ReservationItem>? lines, DateTimeOffset now)
    {
        if (id == Guid.Empty || order == Guid.Empty || lines is null || lines.Count is < 1 or > 50 || lines.Any(x => x is null || x.VariantId == Guid.Empty || x.Quantity < 1) || lines.Select(x => x.VariantId).Distinct().Count() != lines.Count)
            throw new BusinessException("Plan de rezervare invalid.");
        return new()
        {
            Id = id,
            OrderId = order,
            Lines = lines.OrderBy(x => x.VariantId).ToList(),
            Deadline = now.AddMinutes(15),
            NextAttemptAt = now
        };
    }

    public static ReservationPlan CancelBeforeCreation(Guid id, Guid order, DateTimeOffset now)
    {
        if (id == Guid.Empty || order == Guid.Empty)
            throw new BusinessException("Identificator invalid.");
        return new()
        {
            Id = id,
            OrderId = order,
            Deadline = now,
            NextAttemptAt = now,
            CancellationRequested = true,
            Status = "Cancelling"
        };
    }

    public void EnsureSame(ReservationPlan other)
    {
        if (OrderId != other.OrderId || !(CancellationRequested && Lines.Count == 0) && !Lines.SequenceEqual(other.Lines))
            throw new BusinessException("Cheia planului a fost folosită cu alte date.", 409);
    }

    public void Cancel(DateTimeOffset now)
    {
        if (Terminal)
            return;
        CancellationRequested = true;
        Status = "Cancelling";
        NextAttemptAt = now;
    }

    public void Observe(string status, DateTimeOffset expires, DateTimeOffset now)
    {
        Status = status;
        RemoteExpiresAt = expires;
        LastError = null;
        Attempts = 0;
        NextAttemptAt = now.AddSeconds(15);
    }

    public void Failed(DateTimeOffset now)
    {
        Attempts = Math.Min(Attempts + 1, 1000);
        LastError = "InventoryUnavailable";
        NextAttemptAt = now.AddSeconds(Math.Min(60, 5 * Attempts));
    }
}
