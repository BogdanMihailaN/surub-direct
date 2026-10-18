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
public class ProductAdministrationTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task Edit_archive_restore_preserves_commercial_data_and_requires_republication()
    {
        var token = Guid.NewGuid().ToString("N");
        var slug = "lifecycle-" + token;
        Guid id, variant, offer;
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var s = scope.ServiceProvider;
            id = await s.GetRequiredService<CreateProductHandler>().Handle(new("Original " + token, slug, "Initial", "Original description"), default);
            variant = await s.GetRequiredService<AddVariantHandler>().Handle(id, new("SKU-" + token, "M8", 8, 40, "Oțel", "Zincat"), default);
            offer = await s.GetRequiredService<AddOfferHandler>().Handle(id, variant, new("PACK-" + token, "Cutie", 100), default);
            await s.GetRequiredService<SetOfferPriceHandler>().Handle(offer, new(40, 0.21m), default);
            await s.GetRequiredService<ReceiveStockHandler>().Handle(variant, new(Guid.NewGuid(), 250, "TEST"), "test", default);
            await s.GetRequiredService<PublishStoreProductHandler>().Handle(id, default);
            await s.GetRequiredService<UpdateProductHandler>().Handle(id, new("Edited " + token, "Category " + token, "Updated description"), default);
        }
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var s = scope.ServiceProvider;
            var reader = s.GetRequiredService<ICatalogReader>();
            var edited = await reader.GetBySlug(slug, default);
            Assert.NotNull(edited);
            Assert.Equal("Edited " + token, edited.Name);
            Assert.Equal("Updated description", edited.Description);
            Assert.Equal("Published", edited.Status);
            await s.GetRequiredService<ArchiveProductHandler>().Handle(id, default);
            await s.GetRequiredService<ArchiveProductHandler>().Handle(id, default);
        }
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var s = scope.ServiceProvider;
            var reader = s.GetRequiredService<ICatalogReader>();
            Assert.Null(await reader.Get(id, false, default));
            Assert.Null(await reader.GetBySlug(slug, default));
            Assert.Empty((await reader.List(new(Search: token), false, default)).Items);
            Assert.DoesNotContain("Category " + token, await reader.Categories(default));
            var archived = await reader.Get(id, true, default);
            Assert.NotNull(archived);
            Assert.Equal("Archived", archived.Status);
            Assert.Equal(variant, Assert.Single(archived.Variants).Id);
            Assert.Equal(offer, Assert.Single(archived.Variants[0].Offers).Id);
            Assert.True(await reader.VariantExists(variant, default));
            Assert.Equal(250, (await s.GetRequiredService<IInventoryReader>().GetStock([variant], default)).Single().Available);
            Assert.Equal(40, (await s.GetRequiredService<IPricingReader>().GetPrices([offer], default)).Single().NetAmount);
            await Assert.ThrowsAsync<BusinessException>(() => s.GetRequiredService<UpdateProductHandler>().Handle(id, new("Forbidden", "Test", "Test"), default));
            await Assert.ThrowsAsync<BusinessException>(() => s.GetRequiredService<AddOfferHandler>().Handle(id, variant, new("Forbidden", "Cutie", 10), default));
            await s.GetRequiredService<RestoreProductHandler>().Handle(id, default);
            await s.GetRequiredService<RestoreProductHandler>().Handle(id, default);
            Assert.Equal("Draft", (await reader.Get(id, true, default))!.Status);
            Assert.Null(await reader.GetBySlug(slug, default));
            await s.GetRequiredService<PublishStoreProductHandler>().Handle(id, default);
            Assert.NotNull(await reader.GetBySlug(slug, default));
        }
    }

    [Fact]
    public async Task Admin_search_includes_empty_drafts_and_pages_beyond_first_hundred()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var s = scope.ServiceProvider;
        var token = Guid.NewGuid().ToString("N");
        var create = s.GetRequiredService<CreateProductHandler>();
        var ids = new List<Guid>();
        for (var i = 0; i < 101; i++)
            ids.Add(await create.Handle(new($"Pagination {token} {i:D3}", $"page-{token}-{i}", "Test", "Draft without SKU"), default));
        var query = s.GetRequiredService<GetCatalogHandler>();
        var first = await query.Handle(new(Search: token, PageSize: 25, Status: "Draft"), true, default);
        var last = await query.Handle(new(Search: token, Page: 5, PageSize: 25, Status: "Draft"), true, default);
        Assert.Equal(101, first.Total);
        Assert.Equal(25, first.Items.Count);
        Assert.Equal(ids[100], Assert.Single(last.Items).Product.Id);
        Assert.Empty((await query.Handle(new(Search: token), false, default)).Items);
        await s.GetRequiredService<ArchiveProductHandler>().Handle(ids[0], default);
        var archived = await query.Handle(new(Search: token, Status: "Archived"), true, default);
        Assert.Equal(ids[0], Assert.Single(archived.Items).Product.Id);
        Assert.Equal(100, (await query.Handle(new(Search: token, Status: "Draft"), true, default)).Total);
        await Assert.ThrowsAsync<BusinessException>(() => query.Handle(new(Status: "Invalid"), true, default));
    }

    [Fact]
    public async Task Missing_product_mutations_return_not_found()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var s = scope.ServiceProvider;
        var id = Guid.NewGuid();
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => s.GetRequiredService<UpdateProductHandler>().Handle(id, new("Name", "Category", "Description"), default))).Status);
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => s.GetRequiredService<ArchiveProductHandler>().Handle(id, default))).Status);
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => s.GetRequiredService<RestoreProductHandler>().Handle(id, default))).Status);
    }
}
