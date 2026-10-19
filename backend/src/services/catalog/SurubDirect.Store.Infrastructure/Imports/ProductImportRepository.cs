using Microsoft.EntityFrameworkCore;
using Npgsql;
using SurubDirect.BuildingBlocks;
using SurubDirect.Catalog.Infrastructure;
using SurubDirect.Pricing.Infrastructure;
using SurubDirect.Store.Application.Imports;
namespace SurubDirect.Store.Infrastructure;
internal sealed class ProductImportRepository(string connectionString) : IProductImportRepository
{
    public async Task<ImportLookup> Lookup(IReadOnlyList<ImportRow> rows, CancellationToken ct)
    {
        await using var db = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>().UseNpgsql(connectionString).Options);
        var categories = rows.Select(x => x.CategorySlug).Distinct().ToArray();
        var slugs = rows.Select(x => x.Slug).Distinct().ToArray();
        var skus = rows.Select(x => x.Sku).Distinct().ToArray();
        var codes = rows.Select(x => x.OfferCode).Distinct().ToArray();
        return new(await db.Categories.AsNoTracking().Where(x => categories.Contains(x.Slug)).ToListAsync(ct),
            (await db.Products.Where(x => slugs.Contains(x.Slug)).Select(x => x.Slug).ToListAsync(ct)).ToHashSet(),
            (await db.Variants.Where(x => skus.Contains(x.Sku)).Select(x => x.Sku).ToListAsync(ct)).ToHashSet(),
            (await db.Offers.Where(x => codes.Contains(x.Code)).Select(x => x.Code).ToListAsync(ct)).ToHashSet());
    }
    public async Task Save(ImportBatch batch, CancellationToken ct)
    {
        if (!batch.Preview.CanImport) throw new BusinessException("Importul conține erori.");
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var catalog = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>().UseNpgsql(connection).Options);
        await using var pricing = new PricingDbContext(new DbContextOptionsBuilder<PricingDbContext>().UseNpgsql(connection).Options);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await catalog.Database.UseTransactionAsync(transaction, ct);
        await pricing.Database.UseTransactionAsync(transaction, ct);
        // Both schemas belong to Catalog. A failure in either save rolls back the entire import.
        catalog.Categories.AttachRange(batch.Products.Select(x => x.Classification!).DistinctBy(x => x.Id));
        catalog.Products.AddRange(batch.Products);
        pricing.Prices.AddRange(batch.Prices);
        try
        {
            await catalog.SaveChangesAsync(ct);
            await pricing.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new BusinessException("Un slug, SKU sau cod de ambalaj există deja. Importul nu a fost salvat; revalidează fișierul.", 409);
        }
    }
}
