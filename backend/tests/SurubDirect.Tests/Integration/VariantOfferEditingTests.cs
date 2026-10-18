using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Catalog.Application;
using SurubDirect.Catalog.Contracts;
using SurubDirect.Inventory.Application;
using SurubDirect.Inventory.Contracts;
using SurubDirect.Pricing.Application;
using SurubDirect.Pricing.Contracts;
using SurubDirect.Store.Application;
using Xunit;

namespace SurubDirect.Tests;
public class VariantOfferEditingTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    private async Task<(Guid Product, Guid Variant, Guid Offer, string Token)> Create()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var s = scope.ServiceProvider;
        var token = Guid.NewGuid().ToString("N");
        var product = await s.GetRequiredService<CreateProductHandler>().Handle(new("Product " + token, "edit-" + token, "Test", "Description"), default);
        var variant = await s.GetRequiredService<AddVariantHandler>().Handle(product, new("SKU-" + token, "M8 × 40", 8, 40, "Oțel", "Zincat"), default);
        var offer = await s.GetRequiredService<AddOfferHandler>().Handle(product, variant, new("BOX-" + token, "Cutie 100", 100), default);
        await s.GetRequiredService<SetOfferPriceHandler>().Handle(offer, new(40, 0.21m), default);
        await s.GetRequiredService<PublishStoreProductHandler>().Handle(product, default);
        return (product, variant, offer, token);
    }

    [Fact]
    public async Task Edits_persist_and_refresh_search_without_changing_stock_receipts_or_prices()
    {
        var (product, variant, offer, token) = await Create();
        var receipt = new ReceiveStock(Guid.NewGuid(), 250, "BEFORE-EDIT");
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var s = scope.ServiceProvider;
            await s.GetRequiredService<ReceiveStockHandler>().Handle(variant, receipt, "test", default);
            await s.GetRequiredService<UpdateVariantHandler>().Handle(product, variant, new("M10 × 65", 10, 65, "Inox A2", "Natural"), default);
        }
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var s = scope.ServiceProvider;
            var reader = s.GetRequiredService<ICatalogReader>();
            var edited = (await reader.Get(product, false, default))!;
            var item = Assert.Single(edited.Variants);
            Assert.Equal(variant, item.Id);
            Assert.Equal("SKU-" + token.ToUpperInvariant(), item.Sku);
            Assert.Equal("M10 × 65", item.Label);
            Assert.Equal("Inox A2", item.Material);
            Assert.Equal("Natural", item.Finish);
            Assert.Equal(65, item.LengthMm);
            Assert.Empty((await reader.List(new(Search: token, Diameter: 8, Length: 40), false, default)).Items);
            Assert.Equal(product, Assert.Single((await reader.List(new(Search: token, Diameter: 10, Length: 65), false, default)).Items).Id);
            Assert.Contains((await reader.List(new(Search: "M10 x 65", PageSize: 100), false, default)).Items, p => p.Id == product);
            Assert.DoesNotContain((await reader.List(new(Search: "M8 x 40", PageSize: 100), false, default)).Items, p => p.Id == product);
            Assert.True(await s.GetRequiredService<UpdateOfferHandler>().Handle(product, variant, offer, new("Cutie 50", 50, 2, 2), default));
        }
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var s = scope.ServiceProvider;
            var reader = s.GetRequiredService<ICatalogReader>();
            Assert.Null(await reader.GetBySlug("edit-" + token, default));
            var draft = (await reader.Get(product, true, default))!;
            Assert.Equal("Draft", draft.Status);
            var pack = Assert.Single(Assert.Single(draft.Variants).Offers);
            Assert.Equal(offer, pack.Id);
            Assert.Equal("BOX-" + token.ToUpperInvariant(), pack.Code);
            Assert.Equal(50, pack.UnitsPerPack);
            Assert.Equal(2, pack.MinPacks);
            Assert.Equal(2, pack.PackStep);
            Assert.Equal(40, Assert.Single(await s.GetRequiredService<IPricingReader>().GetPrices([offer], default)).NetAmount);
            Assert.False(await s.GetRequiredService<ReceiveStockHandler>().Handle(variant, receipt, "test", default));
            Assert.Equal(250, Assert.Single(await s.GetRequiredService<IInventoryReader>().GetStock([variant], default)).Available);
            await s.GetRequiredService<PublishStoreProductHandler>().Handle(product, default);
            Assert.NotNull(await reader.GetBySlug("edit-" + token, default));
        }
    }

    [Fact]
    public async Task Nested_identifiers_and_archived_products_are_protected()
    {
        var first = await Create();
        var other = await Create();
        await using var scope = fixture.Services.CreateAsyncScope();
        var s = scope.ServiceProvider;
        var variantHandler = s.GetRequiredService<UpdateVariantHandler>();
        var offerHandler = s.GetRequiredService<UpdateOfferHandler>();
        var variant = new UpdateVariant("Changed", 10, 65, "Inox", "Natural");
        var offer = new UpdateOffer("Changed", 50, 1, 1);
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => variantHandler.Handle(Guid.NewGuid(), first.Variant, variant, default))).Status);
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => variantHandler.Handle(first.Product, other.Variant, variant, default))).Status);
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => offerHandler.Handle(first.Product, other.Variant, other.Offer, offer, default))).Status);
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => offerHandler.Handle(first.Product, first.Variant, other.Offer, offer, default))).Status);
        await s.GetRequiredService<ArchiveProductHandler>().Handle(first.Product, default);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => variantHandler.Handle(first.Product, first.Variant, variant, default))).Status);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => offerHandler.Handle(first.Product, first.Variant, first.Offer, offer, default))).Status);
        var reader = s.GetRequiredService<ICatalogReader>();
        var preserved = (await reader.Get(first.Product, true, default))!;
        Assert.Equal("Archived", preserved.Status);
        Assert.Equal("M8 × 40", preserved.Variants[0].Label);
        Assert.Equal(100, preserved.Variants[0].Offers[0].UnitsPerPack);
        Assert.Equal(100, (await reader.Get(other.Product, true, default))!.Variants[0].Offers[0].UnitsPerPack);
    }
}
