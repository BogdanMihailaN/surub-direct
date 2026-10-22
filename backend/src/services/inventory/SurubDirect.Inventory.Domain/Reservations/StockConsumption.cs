namespace SurubDirect.Inventory.Domain;
public sealed class StockConsumption
{
    private StockConsumption()
    {
    }

    public Guid ReservationId { get; private set; }
    public Guid VariantId { get; private set; }
    public Guid OrderId { get; private set; }
    public int Quantity { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static StockConsumption From(StockReservation reservation, ReservationLine line)
    {
        if (reservation.Status != ReservationStatus.Consumed || !reservation.Lines.Contains(line))
            throw new InvalidOperationException("Consumption requires a consumed reservation and its original line.");
        return new()
        {
            ReservationId = reservation.Id,
            VariantId = line.VariantId,
            OrderId = reservation.OrderId,
            Quantity = line.Quantity,
            CreatedAt = reservation.EndedAt ?? throw new InvalidOperationException("Reservation has not ended.")
        };
    }
}
