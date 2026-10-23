using System.Text.Json;

namespace SurubDirect.Ordering.Infrastructure;
public static class ReservationOutcomeParser
{
    public static ReservationOutcome Parse(string? id, string? type, byte[] body)
    {
        if (body.Length > 65536 || type != "inventory.reservation-outcome.v1" || !Guid.TryParse(id, out var eventId) || eventId == Guid.Empty)
            throw new InvalidReservationEventException();
        ReservationOutcome? message;
        try
        {
            message = JsonSerializer.Deserialize<ReservationOutcome>(body);
        }
        catch (JsonException)
        {
            throw new InvalidReservationEventException();
        }

        if (message is null || message.EventId != eventId || message.ReservationId == Guid.Empty || message.OrderId == Guid.Empty || message.Status is not ("Consumed" or "Restored") || message.OccurredAt == default || message.ExpiresAt == default || message.Lines is null || message.Lines.Count is < 1 or > 50 || message.Lines.Any(x => x is null || x.VariantId == Guid.Empty || x.Quantity < 1) || message.Lines.Select(x => x.VariantId).Distinct().Count() != message.Lines.Count)
            throw new InvalidReservationEventException();
        return message;
    }
}
