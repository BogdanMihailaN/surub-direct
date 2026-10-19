using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SurubDirect.Catalog.Infrastructure;
using Testcontainers.PostgreSql;
using Xunit;

namespace SurubDirect.Tests;
public class TaxonomyMigrationTests
{
    [Fact]
    public async Task Populated_legacy_catalog_is_backfilled_without_changing_merchandise()
    {
        await using var container = new PostgreSqlBuilder("postgres:17").Build();
        await container.StartAsync();
        await using var db = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(container.GetConnectionString(), o => o.MigrationsHistoryTable("__EFMigrationsHistory", "catalog")).Options);
        await db.GetService<IMigrator>().MigrateAsync("20261017145714_InitialCatalog");
        var product = Guid.NewGuid();
        var variant = Guid.NewGuid();
        var offer = Guid.NewGuid();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO catalog."Products" ("Id", "Name", "Slug", "Category", "Description", "Status", "CreatedAt")
            VALUES ({product}, 'Existing', 'existing', 'Categorie veche specială', 'Unchanged', 1, now());
            INSERT INTO catalog."Variants" ("Id", "ProductId", "Sku", "Label", "DiameterMm", "LengthMm", "Material", "Finish", "SearchText")
            VALUES ({variant}, {product}, 'OLD-SKU', 'M8', 8, 40, 'Oțel', 'Zincat', 'oldsku');
            INSERT INTO catalog."Offers" ("Id", "VariantId", "Code", "Label", "UnitsPerPack", "MinPacks", "PackStep")
            VALUES ({offer}, {variant}, 'OLD-BOX', 'Cutie 100', 100, 2, 2);
            """);
        await db.Database.MigrateAsync();
        var migrated = await db.Products.Include(x => x.Classification).Include(x => x.Variants).ThenInclude(x => x.Offers).SingleAsync();
        Assert.Equal(product, migrated.Id);
        Assert.Equal("existing", migrated.Slug);
        Assert.Equal("Unchanged", migrated.Description);
        Assert.Equal("Published", migrated.Status.ToString());
        Assert.Equal("Categorie veche specială", migrated.Classification!.Name);
        Assert.NotEqual(Guid.Empty, migrated.CategoryId);
        var sku = Assert.Single(migrated.Variants);
        Assert.Equal(variant, sku.Id);
        Assert.Equal("OLD-SKU", sku.Sku);
        Assert.Equal(8, sku.DiameterMm);
        Assert.Equal(40, sku.LengthMm);
        Assert.Equal(offer, Assert.Single(sku.Offers).Id);
        Assert.Equal(100, sku.Offers[0].UnitsPerPack);
        Assert.Empty(await db.VariantAttributeValues.ToListAsync());
        Assert.Equal(4, await db.CategoryAttributes.CountAsync(x => x.CategoryId == migrated.CategoryId));
    }
}
