using Microsoft.EntityFrameworkCore;
using SurubDirect.BuildingBlocks;
using SurubDirect.Inventory.Application;
using SurubDirect.Inventory.Domain;
namespace SurubDirect.Inventory.Infrastructure;

internal sealed class ReceiptCorrections(InventoryDbContext db) : IReceiptCorrections
{
    public async Task<bool> Apply(ReceiptCorrection c, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var receipt = await db.Receipts.FromSqlInterpolated($"SELECT * FROM inventory.\"Receipts\" WHERE \"Id\" = {c.ReceiptId} FOR UPDATE").AsNoTracking().SingleOrDefaultAsync(ct) ?? throw new BusinessException("Recepția originală nu există.", 404);
        if (receipt.VariantId != c.VariantId) throw new BusinessException("SKU diferit de recepția originală.", 409);
        var old = await db.ReceiptCorrections.AsNoTracking().SingleOrDefaultAsync(x => x.Id == c.Id, ct);
        if (old is not null) { old.EnsureSame(c); await tx.CommitAsync(ct); return false; }
        var corrected = await db.ReceiptCorrections.Where(x => x.ReceiptId == c.ReceiptId).SumAsync(x => (long)x.Quantity, ct);
        if (corrected + c.Quantity > receipt.Quantity) throw new BusinessException("Corecția depășește cantitatea recepției.", 409);
        if (await db.ReceiptCorrections.AnyAsync(x => x.ReceiptId == c.ReceiptId && x.Reference == c.Reference, ct)) throw new BusinessException("Documentul corecției există deja.", 409);
        var inserted = await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO inventory.\"ReceiptCorrections\" (\"Id\",\"ReceiptId\",\"VariantId\",\"Quantity\",\"Reference\",\"Reason\",\"Actor\",\"CreatedAt\") VALUES ({c.Id},{c.ReceiptId},{c.VariantId},{c.Quantity},{c.Reference},{c.Reason},{c.Actor},{c.CreatedAt}) ON CONFLICT (\"Id\") DO NOTHING", ct);
        if (inserted == 0) { (await db.ReceiptCorrections.AsNoTracking().SingleAsync(x => x.Id == c.Id, ct)).EnsureSame(c); await tx.CommitAsync(ct); return false; }
        var balance = await db.Balances.FromSqlInterpolated($"SELECT * FROM inventory.\"Balances\" WHERE \"VariantId\" = {c.VariantId} FOR UPDATE").AsNoTracking().SingleOrDefaultAsync(ct);
        if (balance is null || balance.OnHand - balance.Reserved < c.Quantity) throw new BusinessException("Stoc insuficient sau rezervat pentru retragerea corecției.", 409);
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE inventory.\"Balances\" SET \"OnHand\" = \"OnHand\" - {c.Quantity} WHERE \"VariantId\" = {c.VariantId}", ct);
        await tx.CommitAsync(ct); return true;
    }
}
