using Microsoft.EntityFrameworkCore;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Infrastructure;
internal sealed class MessagingAdministration(OrderingDbContext db) : IMessagingAdministration
{
    public async Task<MessagingStatusDto> Read(int page, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, ct);
        var query = db.FailedReservationMessages.AsNoTracking();
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CapturedAt).ThenBy(x => x.Id).Skip((page - 1) * 25).Take(25).ToListAsync(ct);
        var result = new MessagingStatusDto(await db.ReservationInbox.CountAsync(ct), await db.ReservationInbox.MaxAsync(x => (DateTimeOffset? )x.ReceivedAt, ct), await query.CountAsync(x => x.RequestedAt == null, ct), await query.CountAsync(x => x.RequestedAt != null && x.PublishedAt == null, ct), items.Select(FailedMessageProjection.Map).ToArray(), total, page, 25);
        await tx.CommitAsync(ct);
        return result;
    }

    public async Task<FailedMessageDto> Replay(Guid id, RequestMessageReplay request, string actor, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({request.RequestId.ToString()},4))", ct);
        var used = await db.FailedReservationMessages.AsNoTracking().SingleOrDefaultAsync(x => x.ReplayRequestId == request.RequestId, ct);
        if (used is not null && used.Id != id)
            throw new BusinessException("Identificatorul cererii a fost folosit pentru alt mesaj.", 409);
        var row = await db.FailedReservationMessages.FromSqlInterpolated($"""SELECT * FROM ordering."FailedReservationMessages" WHERE "Id"={id} FOR UPDATE""").SingleOrDefaultAsync(ct) ?? throw new BusinessException("Mesajul nu există.", 404);
        if (row.ReplayRequestId is not null)
        {
            if (row.ReplayRequestId != request.RequestId || row.ReplayReason != request.Reason)
                throw new BusinessException("Reluarea a fost deja solicitată. Actualizează lista.", 409);
        }
        else
        {
            if (!row.CanReplay)
                throw new BusinessException("Contractul mesajului nu permite reluarea. Este necesară investigarea tehnică.", 409);
            row.ReplayRequestId = request.RequestId;
            row.ReplayReason = request.Reason;
            row.RequestedBy = actor;
            row.RequestedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        await tx.CommitAsync(ct);
        return FailedMessageProjection.Map(row);
    }
}
