using System.Text.Json;
using SurubDirect.Inventory.Domain;

namespace SurubDirect.Inventory.Infrastructure;
public sealed class InventoryEvent
{
    public Guid Id { get; set; }
    public Guid ReservationId { get; set; }
    public string Status { get; set; } = "";
    public string Payload { get; set; } = "";
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }

    public static InventoryEvent From(StockReservation reservation, DateTimeOffset now)
    {
        var id = Guid.NewGuid();
        var value = new ReservationOutcome(id, reservation.Id, reservation.OrderId, reservation.Status.ToString(), reservation.ExpiresAt, now, reservation.Lines.Select(x => new ReservationOutcomeLine(x.VariantId, x.Quantity)).ToArray());
        return new()
        {
            Id = id,
            ReservationId = reservation.Id,
            Status = value.Status,
            Payload = JsonSerializer.Serialize(value),
            OccurredAt = now
        };
    }
}
