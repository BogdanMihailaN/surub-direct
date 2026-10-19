using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Catalog.Domain;
using SurubDirect.Catalog.Infrastructure;
using SurubDirect.Pricing.Domain;
using SurubDirect.Pricing.Infrastructure;
using SurubDirect.Store.Application.Imports;
using Xunit;
namespace SurubDirect.Tests;
public sealed class ProductImportTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    private async Task<(string Csv, string Key)> File()
    {
        var key = "csv-" + Guid.NewGuid().ToString("N");
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        db.Categories.Add(Category.Create(key, key, null, 0));
        await db.SaveChangesAsync();
        var row = $"Produs;{key};{key};Descriere;{key};M6 x 20;6;20;Oțel;Zincat;{key}-100;Cutie;100;1;1;10.50;0.21";
        return (ImportCsvFormat.Header + "\n" + row + "\n" + row.Replace(key + "-100;Cutie;100", key + "-10;Pungă;10"), key);
    }
    [Fact]
    public async Task Preview_is_read_only_commit_creates_drafts_with_shared_sku_and_prices_retry_is_rejected()
    {
        var (csv, key) = await File();
        await using var scope = fixture.Services.CreateAsyncScope(); var services = scope.ServiceProvider;
        var preview = await services.GetRequiredService<PreviewImportHandler>().Handle(csv, default);
        Assert.True(preview.CanImport); Assert.Equal(1, preview.ProductCount); Assert.Equal(1, preview.VariantCount); Assert.Equal(2, preview.OfferCount);
        var db = services.GetRequiredService<CatalogDbContext>();
        Assert.False(await db.Products.AnyAsync(x => x.Slug == key));
        var result = await services.GetRequiredService<CommitImportHandler>().Handle(csv, default);
        var product = await db.Products.Include(x => x.Variants).ThenInclude(x => x.Offers).SingleAsync(x => x.Slug == key);
        Assert.Equal(ProductStatus.Draft, product.Status); Assert.Equal(result.ProductIds[0], product.Id);
        var variant = Assert.Single(product.Variants); Assert.Equal(2, variant.Offers.Count);
        var ids = variant.Offers.Select(x => x.Id).ToArray();
        var prices = await services.GetRequiredService<PricingDbContext>().Prices.Where(x => ids.Contains(x.OfferId)).ToListAsync();
        Assert.Equal(2, prices.Count); Assert.All(prices, p => { Assert.Equal(10.50m, p.NetAmount); Assert.Equal(0.21m, p.TaxRate); });
        await Assert.ThrowsAsync<BusinessException>(() => services.GetRequiredService<CommitImportHandler>().Handle(csv, default));
        Assert.Equal(1, await db.Products.CountAsync(x => x.Slug == key));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Existing_sku_or_offer_is_rejected_even_for_a_new_product(bool existingOffer)
    {
        var (csv, key) = await File();
        await using var scope = fixture.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        await services.GetRequiredService<CommitImportHandler>().Handle(csv, default);
        var lines = csv.Split('\n');
        for (var i = 1; i < lines.Length; i++)
        {
            var fields = lines[i].Split(';'); fields[1] = key + "-new";
            if (existingOffer) fields[4] = key + "-new-sku";
            lines[i] = string.Join(';', fields);
        }
        var preview = await services.GetRequiredService<PreviewImportHandler>().Handle(string.Join('\n', lines), default);
        Assert.False(preview.CanImport);
        Assert.Equal(2, preview.Errors.Count);
        Assert.All(preview.Errors, e => Assert.Contains(existingOffer ? "Codul ambalajului" : "SKU-ul", e.Message));
    }

    [Theory]
    [InlineData("price")]
    [InlineData("product")]
    [InlineData("sku")]
    [InlineData("code")]
    [InlineData("category")]
    public async Task Invalid_row_blocks_the_entire_file_and_reports_the_line(string issue)
    {
        var (csv, key) = await File(); var lines = csv.Split('\n');
        lines[2] = issue switch
        {
            "price" => lines[2].Replace("10.50", "-10.50"),
            "product" => lines[2].Replace("Produs;", "Alt produs;"),
            "sku" => lines[2].Replace("M6 x 20", "M8 x 40"),
            "code" => lines[2].Replace(key + "-10;", key + "-100;"),
            _ => lines[2].Replace($";{key};Descriere", ";absent;Descriere")
        };
        csv = string.Join('\n', lines);
        await using var scope = fixture.Services.CreateAsyncScope(); var services = scope.ServiceProvider;
        var preview = await services.GetRequiredService<PreviewImportHandler>().Handle(csv, default);
        Assert.False(preview.CanImport); Assert.Equal(3, Assert.Single(preview.Errors).Line);
        await Assert.ThrowsAsync<BusinessException>(() => services.GetRequiredService<CommitImportHandler>().Handle(csv, default));
        Assert.False(await services.GetRequiredService<CatalogDbContext>().Products.AnyAsync(x => x.Slug == key));
    }
    [Fact]
    public async Task Pricing_failure_rolls_back_products_variants_and_offers()
    {
        var (csv, key) = await File();
        await using var scope = fixture.Services.CreateAsyncScope(); var services = scope.ServiceProvider;
        var batch = await services.GetRequiredService<ImportBatchBuilder>().Build(csv, default);
        var prices = services.GetRequiredService<PricingDbContext>();
        var collision = Guid.NewGuid(); prices.Prices.Add(OfferPrice.Create(collision, 1, 0)); await prices.SaveChangesAsync();
        batch = batch with { Prices = [OfferPrice.Create(collision, 5, 0)] };
        await Assert.ThrowsAsync<BusinessException>(() => services.GetRequiredService<IProductImportRepository>().Save(batch, default));
        var db = services.GetRequiredService<CatalogDbContext>();
        Assert.False(await db.Products.AnyAsync(x => x.Slug == key));
        Assert.False(await db.Variants.AnyAsync(x => x.Sku == key.ToUpperInvariant()));
        Assert.False(await db.Offers.AnyAsync(x => x.Code == (key + "-100").ToUpperInvariant()));
        Assert.Equal(1m, (await prices.Prices.SingleAsync(x => x.OfferId == collision)).NetAmount);
    }
    [Fact]
    public async Task Two_prevalidated_concurrent_imports_cannot_create_duplicates()
    {
        var (csv, key) = await File();
        await using var first = fixture.Services.CreateAsyncScope(); await using var second = fixture.Services.CreateAsyncScope();
        var a = await first.ServiceProvider.GetRequiredService<ImportBatchBuilder>().Build(csv, default);
        var b = await second.ServiceProvider.GetRequiredService<ImportBatchBuilder>().Build(csv, default);
        async Task<bool> Save(IServiceProvider services, ImportBatch batch)
        {
            try { await services.GetRequiredService<IProductImportRepository>().Save(batch, default); return true; }
            catch (BusinessException e) { Assert.Equal(409, e.Status); return false; }
        }
        var results = await Task.WhenAll(Save(first.ServiceProvider, a), Save(second.ServiceProvider, b));
        Assert.Single(results, x => x);
        var db = first.ServiceProvider.GetRequiredService<CatalogDbContext>();
        Assert.Equal(1, await db.Products.CountAsync(x => x.Slug == key));
        Assert.Equal(1, await db.Variants.CountAsync(x => x.Sku == key.ToUpperInvariant()));
    }
}
