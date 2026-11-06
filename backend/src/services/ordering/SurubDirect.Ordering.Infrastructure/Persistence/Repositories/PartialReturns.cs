using Microsoft.EntityFrameworkCore;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Domain;
using SurubDirect.Ordering.Contracts;
namespace SurubDirect.Ordering.Infrastructure;
internal sealed class PartialReturns(OrderingDbContext db, TimeProvider clock) : IPartialReturns, IAccountPartialReturns
{
    private async Task<DemoOrder> LockOrder(Guid id, Guid requestId, CancellationToken ct)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({requestId.ToString()}, 2))", ct);
        _ = await db.ReservationPlans.FromSqlInterpolated($"""SELECT * FROM ordering."ReservationPlans" WHERE "Id"={id} FOR UPDATE""").SingleOrDefaultAsync(ct)
            ?? throw new BusinessException("Comanda nu există.", 404);
        return await db.DemoOrders.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new BusinessException("Comanda nu există.", 404);
    }
    public Task<PartialReturnRequest> Request(Guid orderId, Guid requestId, IReadOnlyList<PartialReturnSelection>? lines, string? reason, string actor, CancellationToken ct)
        => RequestCore(orderId, requestId, lines, reason, actor, null, ct);
    public Task<PartialReturnRequest> Request(string accountId, Guid orderId, Guid requestId, IReadOnlyList<PartialReturnSelection>? lines, string? reason, CancellationToken ct)
        => RequestCore(orderId, requestId, lines, reason, "customer:" + accountId, accountId, ct);
    private async Task<PartialReturnRequest> RequestCore(Guid orderId, Guid requestId, IReadOnlyList<PartialReturnSelection>? lines, string? reason, string actor, string? accountId, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var order = await LockOrder(orderId, requestId, ct);
        if (accountId is not null && order.AccountId != accountId) throw new BusinessException("Comanda nu este accesibilă.", 404);
        var candidate = PartialReturnRequest.Create(requestId, order, lines, reason, actor, clock.GetUtcNow());
        var previous = await db.PartialReturns.SingleOrDefaultAsync(x => x.Id == requestId, ct);
        if (previous is not null) { previous.EnsureSame(candidate); await tx.CommitAsync(ct); return previous; }
        if (order.Status != "Confirmed" || order.ShipmentStatus != "Delivered" || order.ReturnStatus != "NotRequested")
            throw new BusinessException("Returul pe cantități necesită o comandă livrată fără un retur integral început.", 409);
        if (await db.PartialReturns.CountAsync(x => x.OrderId == orderId, ct) >= 100)
            throw new BusinessException("Limita cererilor demonstrative pentru această comandă a fost atinsă.", 409);
        var committed = await db.PartialReturns.AsNoTracking().Where(x => x.OrderId == orderId && x.Status != "Rejected").SelectMany(x => x.Lines).ToListAsync(ct);
        foreach (var line in candidate.Lines)
            if (committed.Where(x => x.OfferId == line.OfferId).Sum(x => (long)x.Packs) + line.Packs > order.Lines.Single(x => x.OfferId == line.OfferId).Packs)
                throw new BusinessException("Cantitatea este deja inclusă în alte cereri de retur active.", 409);
        db.PartialReturns.Add(candidate);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return candidate;
    }
    public async Task<IReadOnlyList<PartialReturnRequest>> Read(Guid orderId, CancellationToken ct)
    {
        if (!await db.DemoOrders.AnyAsync(x => x.Id == orderId, ct)) throw new BusinessException("Comanda nu există.", 404);
        return await db.PartialReturns.AsNoTracking().Where(x => x.OrderId == orderId).OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).ToListAsync(ct);
    }
    public async Task<AccountPartialReturnsDto> Read(string accountId, Guid orderId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, ct);
        var order = await db.DemoOrders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == orderId && x.AccountId == accountId, ct)
            ?? throw new BusinessException("Comanda nu este accesibilă.", 404);
        var requests = await db.PartialReturns.AsNoTracking().Where(x => x.OrderId == orderId).OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).ToListAsync(ct);
        var committed = requests.Where(x => x.Status != "Rejected").SelectMany(x => x.Lines).ToArray();
        var offers = order.Lines.Select(line => new AccountReturnOfferDto(line.OfferId, line.ProductName, line.Sku, line.OfferLabel, line.Packs,
            line.Packs - committed.Where(x => x.OfferId == line.OfferId).Sum(x => x.Packs), line.UnitsPerPack)).ToArray();
        var canRequest = order.Status == "Confirmed" && order.ShipmentStatus == "Delivered" && order.ReturnStatus == "NotRequested" && requests.Count < 100 && offers.Any(x => x.AvailablePacks > 0);
        await tx.CommitAsync(ct);
        return new(canRequest, offers, requests.Select(AccountPartialReturnProjection.Map).ToArray());
    }
    public async Task Decide(Guid orderId, Guid requestId, Guid operationId, long expectedVersion, string? target, string? reason, string actor, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await LockOrder(orderId, requestId, ct);
        var request = await db.PartialReturns.SingleOrDefaultAsync(x => x.Id == requestId && x.OrderId == orderId, ct)
            ?? throw new BusinessException("Cererea nu există pentru această comandă.", 404);
        request.Decide(operationId, expectedVersion, target, reason, actor, clock.GetUtcNow());
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
}
