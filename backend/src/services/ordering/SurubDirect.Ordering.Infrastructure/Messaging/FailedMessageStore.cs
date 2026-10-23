using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SurubDirect.Messaging;

namespace SurubDirect.Ordering.Infrastructure;
public sealed class FailedMessageStore(OrderingDbContext db)
{
    public async Task Capture(BrokerDelivery message, CancellationToken ct)
    {
        var fingerprint = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new { message.MessageId, message.Type, message.ReplayId, Hash = Convert.ToHexString(SHA256.HashData(message.Body)) })));
        var valid = true;
        try
        {
            ReservationOutcomeParser.Parse(message.MessageId, message.Type, message.Body);
        }
        catch (InvalidReservationEventException)
        {
            valid = false;
        }

        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({fingerprint},5))", ct);
        if (!await db.FailedReservationMessages.AnyAsync(x => x.Fingerprint == fingerprint, ct))
        {
            db.FailedReservationMessages.Add(new() { Id = Guid.NewGuid(), Fingerprint = fingerprint, MessageId = message.MessageId?[..Math.Min(200, message.MessageId.Length)], MessageType = message.Type?[..Math.Min(200, message.Type.Length)], PreviousReplayId = message.ReplayId, Payload = message.Body, OriginalSize = message.Body.Length, Attempts = message.Attempt, CanReplay = valid, CapturedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync(ct);
        }

        await tx.CommitAsync(ct);
    }

    public async Task<int> PublishPending(Func<BrokerDelivery, CancellationToken, Task> publish, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var row = await db.FailedReservationMessages.FromSqlRaw("""SELECT * FROM ordering."FailedReservationMessages" WHERE "RequestedAt" IS NOT NULL AND "PublishedAt" IS NULL ORDER BY "RequestedAt","Id" LIMIT 1 FOR UPDATE SKIP LOCKED""").SingleOrDefaultAsync(ct);
        if (row is null)
            return 0;
        await publish(new(0, row.MessageId, row.MessageType, row.Payload, 0, row.ReplayRequestId), ct);
        row.PublishedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return 1;
    }
}
