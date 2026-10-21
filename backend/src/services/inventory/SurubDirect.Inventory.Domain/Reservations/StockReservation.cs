using SurubDirect.BuildingBlocks;

namespace SurubDirect.Inventory.Domain;
public sealed class StockReservation
{
    private StockReservation()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public int DurationSeconds { get; private set; }
    public List<ReservationLine> Lines { get; private set; } = [];
    public ReservationStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }

    public static StockReservation Create(Guid id, Guid orderId, int duration, IReadOnlyList<ReservationLine>? lines, DateTimeOffset now)
    {
        ValidateIds(id, orderId);
        if (duration is < 60 or > 1800)
            throw new BusinessException("Durata rezervării trebuie să fie între 60 și 1800 secunde.");
        if (lines is null || lines.Count is < 1 or > 50 || lines.Any(x => x is null || x.VariantId == Guid.Empty || x.Quantity < 1) || lines.Select(x => x.VariantId).Distinct().Count() != lines.Count)
            throw new BusinessException("Rezervarea necesită 1–50 SKU-uri distincte și cantități pozitive.");
        return new()
        {
            Id = id,
            OrderId = orderId,
            DurationSeconds = duration,
            Lines = lines.OrderBy(x => x.VariantId).ToList(),
            CreatedAt = now,
            ExpiresAt = now.AddSeconds(duration),
            Status = ReservationStatus.Active
        };
    }

    public static StockReservation CancelBeforeCreation(Guid id, Guid orderId, DateTimeOffset now)
    {
        ValidateIds(id, orderId);
        return new()
        {
            Id = id,
            OrderId = orderId,
            CreatedAt = now,
            ExpiresAt = now,
            EndedAt = now,
            Status = ReservationStatus.Released
        };
    }

    private static void ValidateIds(Guid id, Guid order)
    {
        if (id == Guid.Empty || order == Guid.Empty)
            throw new BusinessException("Identificator rezervare sau comandă invalid.");
    }

    public void EnsureSame(StockReservation other)
    {
        EnsureOrder(other.OrderId);
        // An early cancellation permanently fences the id, before its payload is known.
        if (DurationSeconds == 0)
            return;
        if (DurationSeconds != other.DurationSeconds || !Lines.SequenceEqual(other.Lines))
            throw new BusinessException("Cheia rezervării a fost folosită cu alte date.", 409);
    }

    public void EnsureOrder(Guid orderId)
    {
        if (OrderId != orderId)
            throw new BusinessException("Rezervarea aparține altei comenzi.", 409);
    }

    public bool End(DateTimeOffset now)
    {
        if (Status != ReservationStatus.Active)
            return false;
        Status = ExpiresAt <= now ? ReservationStatus.Expired : ReservationStatus.Released;
        EndedAt = now;
        return true;
    }
}
