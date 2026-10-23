using Microsoft.EntityFrameworkCore;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Infrastructure;
internal sealed class MessageFailureMonitor(OrderingDbContext db) : IMessageFailureMonitor
{
    public async Task<MessageFailureSummary> Read(CancellationToken ct)
    {
        // Aggregate in SQL; never load failed message bodies for monitoring.
        return await db.FailedReservationMessages.GroupBy(x => 1).Select(g => new MessageFailureSummary(g.Count(x => x.RequestedAt == null), g.Count(x => x.RequestedAt != null && x.PublishedAt == null))).SingleOrDefaultAsync(ct) ?? new(0, 0);
    }
}
