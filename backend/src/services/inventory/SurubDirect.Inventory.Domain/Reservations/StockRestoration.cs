namespace SurubDirect.Inventory.Domain;
public sealed class StockRestoration
{
    private StockRestoration()
    {
    }

    public Guid ReservationId { get; private set; }
    public Guid VariantId { get; private set; }
    public Guid OrderId { get; private set; }
    public int Quantity { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static StockRestoration From(StockReservation reservation, ReservationLine line, DateTimeOffset now)
    {
        if (reservation.Status != ReservationStatus.Restored || !reservation.Lines.Contains(line))
            throw new InvalidOperationException("Restoration requires a restored reservation and its original line.");
        return new()
        {
            ReservationId = reservation.Id,
            VariantId = line.VariantId,
            OrderId = reservation.OrderId,
            Quantity = line.Quantity,
            CreatedAt = now
        };
    }
}
