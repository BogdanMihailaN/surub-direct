using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Infrastructure;
internal sealed class ExpiredCartCleanup(OrderingDbContext db, TimeProvider clock) : IExpiredCartCleanup
{
    private const string CandidatesSql = """
        SELECT c."Id" AS "Value" FROM ordering."Carts" c
        WHERE c."ExpiresAt" <= {0} AND c."PreparedId" IS NULL
          AND NOT EXISTS (SELECT 1 FROM ordering."PreparedCarts" p WHERE p."CartId" = c."Id")
          AND NOT EXISTS (SELECT 1 FROM ordering."DemoOrders" o WHERE o."CartId" = c."Id")
        ORDER BY c."ExpiresAt", c."Id" LIMIT {1}
        """;
    public async Task<ExpiredCartCleanupResult> Run(int graceDays, int batchSize, bool dryRun, CancellationToken ct)
    {
        if (graceDays is < 1 or > 3650)
            throw new ArgumentOutOfRangeException(nameof(graceDays));
        if (batchSize is < 1 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(batchSize));
        var cutoff = clock.GetUtcNow().AddDays(-graceDays);
        Task<List<Guid>> Candidates(bool locked) => db.Database.SqlQuery<Guid>(FormattableStringFactory.Create(CandidatesSql + (locked ? " FOR UPDATE OF c SKIP LOCKED" : ""), cutoff, batchSize)).ToListAsync(ct);
        if (dryRun)
            return new((await Candidates(false)).Count, 0, true);
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var ids = await Candidates(true);
        var removed = ids.Count == 0 ? 0 : await db.Carts.Where(x => ids.Contains(x.Id)).ExecuteDeleteAsync(ct);
        await tx.CommitAsync(ct);
        return new(ids.Count, removed, false);
    }
}
