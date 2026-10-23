using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SurubDirect.Ordering.Domain;

namespace SurubDirect.Ordering.Infrastructure;
public sealed class ReservationInbox(OrderingDbContext db)
{
    public async Task Handle(string? id, string? type, byte[] body, CancellationToken ct)
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
        var hash = Convert.ToHexString(SHA256.HashData(body));
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({eventId.ToString()}, 3))", ct);
        var existing = await db.ReservationInbox.SingleOrDefaultAsync(x => x.Id == eventId, ct);
        if (existing is not null)
        {
            if (existing.PayloadHash != hash)
                throw new InvalidReservationEventException();
            await tx.CommitAsync(ct);
            return;
        }

        var plan = await db.ReservationPlans.FromSqlInterpolated($"""SELECT * FROM ordering."ReservationPlans" WHERE "Id"={message.ReservationId} FOR UPDATE""").SingleOrDefaultAsync(ct);
        if (plan is not null)
        {
            if (plan.OrderId != message.OrderId || !plan.Lines.SequenceEqual(message.Lines.OrderBy(x => x.VariantId).Select(x => new ReservationItem(x.VariantId, x.Quantity))))
                throw new InvalidReservationEventException();
            var now = DateTimeOffset.UtcNow;
            // Notifications only advance a locally authorized intent; late consumption never reverses restoration.
            if (plan.ObserveOutcome(message.Status, message.ExpiresAt, now))
            {
                var order = await db.DemoOrders.SingleOrDefaultAsync(x => x.Id == plan.Id, ct);
                order?.Reconcile(plan, now);
            }
        }

        // Unknown plans are valid for Inventory's other callers; never create a local order from an event.
        db.ReservationInbox.Add(new() { Id = eventId, PayloadHash = hash, ReceivedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
