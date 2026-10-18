using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using SurubDirect.BuildingBlocks;
using SurubDirect.Catalog.Application;
using SurubDirect.Catalog.Contracts;
using SurubDirect.Catalog.Infrastructure;
using SurubDirect.Pricing.Application;
using SurubDirect.Inventory.Application;
using SurubDirect.Inventory.Contracts;
using SurubDirect.Store.Application;

namespace SurubDirect.Tests;
public class DatabaseTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    private async Task<(Guid Product, Guid Variant)> Create(string label = "M8", decimal diameter = 8, decimal length = 40)
    {
        await using var s = fixture.Services.CreateAsyncScope();
        var create = s.ServiceProvider.GetRequiredService<CreateProductHandler>();
        var variant = s.ServiceProvider.GetRequiredService<AddVariantHandler>();
        var token = Guid.NewGuid().ToString("N");
        var p = await create.Handle(new("Test " + token, "test-" + token, "Test", "Descriere pentru teste"), default);
        var v = await variant.Handle(p, new("SKU-" + token, label, diameter, length, "Oțel", "Zincat"), default);
        return (p, v);
    }

    private async Task<bool> Receive(Guid variant, ReceiveStock receipt)
    {
        await using var s = fixture.Services.CreateAsyncScope();
        return await s.ServiceProvider.GetRequiredService<ReceiveStockHandler>().Handle(variant, receipt, "test", default);
    }

    private async Task<long> Available(Guid variant)
    {
        await using var s = fixture.Services.CreateAsyncScope();
        var values = await s.ServiceProvider.GetRequiredService<IInventoryReader>().GetStock([variant], default);
        return values.Single().Available;
    }

    [Fact]
    public async Task Concurrent_duplicate_receipt_increments_once()
    {
        var(_, v) = await Create();
        var receipt = new ReceiveStock(Guid.NewGuid(), 200, "RECEIPT-1");
        var results = await Task.WhenAll(Receive(v, receipt), Receive(v, receipt));
        Assert.Single(results, x => x);
        Assert.Equal(200, await Available(v));
    }

    [Fact]
    public async Task Concurrent_different_receipts_do_not_lose_updates()
    {
        var(_, v) = await Create();
        await Task.WhenAll(Enumerable.Range(0, 8).Select(i => Receive(v, new(Guid.NewGuid(), 25, "RECEIPT-" + i))));
        Assert.Equal(200, await Available(v));
    }

    [Fact]
    public async Task Reusing_receipt_id_with_changed_payload_is_rejected()
    {
        var(_, v) = await Create();
        var id = Guid.NewGuid();
        await Receive(v, new(id, 10, "REF"));
        var error = await Assert.ThrowsAsync<BusinessException>(() => Receive(v, new(id, 20, "REF")));
        Assert.Equal(409, error.Status);
        Assert.Equal(10, await Available(v));
    }

    [Fact]
    public async Task Publish_requires_price_and_outbox_projects_once()
    {
        var(p, v) = await Create();
        await using var scope = fixture.Services.CreateAsyncScope();
        var s = scope.ServiceProvider;
        var offerHandler = s.GetRequiredService<AddOfferHandler>();
        var store = s.GetRequiredService<PublishStoreProductHandler>();
        var offer = await offerHandler.Handle(p, v, new("O-" + Guid.NewGuid(), "Cutie", 100), default);
        await Assert.ThrowsAsync<BusinessException>(() => store.Handle(p, default));
        await s.GetRequiredService<SetOfferPriceHandler>().Handle(offer, new(45, 0.21m), default);
        await store.Handle(p, default);
        await store.Handle(p, default);
        var db = s.GetRequiredService<CatalogDbContext>();
        Assert.Equal(1, await db.Outbox.CountAsync(x => x.ProductId == p));
        var processor = s.GetRequiredService<ICatalogOutbox>();
        await processor.ProcessBatch(default);
        await processor.ProcessBatch(default);
        Assert.Equal(1, await db.Activities.CountAsync(x => x.ProductId == p));
        Assert.NotNull((await db.Outbox.SingleAsync(x => x.ProductId == p)).ProcessedAt);
    }

    [Fact]
    public async Task Filters_must_match_one_variant_and_drafts_are_hidden()
    {
        var(p, v) = await Create("7 × 40", 7, 40);
        await using var scope = fixture.Services.CreateAsyncScope();
        var s = scope.ServiceProvider;
        var variantHandler = s.GetRequiredService<AddVariantHandler>();
        var offerHandler = s.GetRequiredService<AddOfferHandler>();
        var other = await variantHandler.Handle(p, new("OTHER-" + Guid.NewGuid(), "8 × 60", 8, 60, "Oțel", "Zincat"), default);
        var first = await offerHandler.Handle(p, v, new("FIRST-" + Guid.NewGuid(), "Cutie", 100), default);
        var second = await offerHandler.Handle(p, other, new("SECOND-" + Guid.NewGuid(), "Cutie", 100), default);
        var reader = s.GetRequiredService<ICatalogReader>();
        Assert.Null(await reader.Get(p, false, default));
        var prices = s.GetRequiredService<SetOfferPriceHandler>();
        await prices.Handle(first, new(10, 0), default);
        await prices.Handle(second, new(12, 0), default);
        await s.GetRequiredService<PublishStoreProductHandler>().Handle(p, default);
        var incorrect = await reader.List(new(Diameter: 7, Length: 60, PageSize: 100), false, default);
        Assert.DoesNotContain(incorrect.Items, x => x.Id == p);
        var correct = await reader.List(new(Diameter: 8, Length: 60, PageSize: 100), false, default);
        Assert.Contains(correct.Items, x => x.Id == p);
    }

    [Fact]
    public async Task Duplicate_sku_is_rejected_by_database()
    {
        var(p, _) = await Create();
        await using var scope = fixture.Services.CreateAsyncScope();
        var variantHandler = scope.ServiceProvider.GetRequiredService<AddVariantHandler>();
        var sku = "UNIQUE-" + Guid.NewGuid();
        await variantHandler.Handle(p, new(sku, "A", 1, 1, "Oțel", "Zincat"), default);
        var(other, _) = await Create();
        await Assert.ThrowsAsync<DbUpdateException>(() => variantHandler.Handle(other, new(sku, "B", 2, 2, "Oțel", "Zincat"), default));
    }
}
