using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Catalog.Application;
using SurubDirect.Catalog.Contracts;
using SurubDirect.Pricing.Application;
using SurubDirect.Store.Application;
using Xunit;
namespace SurubDirect.Tests;
public class ProductImagePersistenceTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    private async Task<(Guid Product, Guid Variant)> Create()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var s = scope.ServiceProvider;
        var token = Guid.NewGuid().ToString("N");
        var product = await s.GetRequiredService<CreateProductHandler>().Handle(new("Image " + token, "image-" + token, "Images", "Description"), default);
        var variant = await s.GetRequiredService<AddVariantHandler>().Handle(product, new("IMG-" + token, "M8", 8, 40, "Oțel", "Zincat"), default);
        var offer = await s.GetRequiredService<AddOfferHandler>().Handle(product, variant, new("BOX-" + token, "Cutie", 100), default);
        await s.GetRequiredService<SetOfferPriceHandler>().Handle(offer, new(40, 0.21m), default);
        return (product, variant);
    }
    private async Task<Guid> Upload(Guid product, Guid? variant = null)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<UploadImageHandler>().Handle(product, new MemoryStream(TestImage.Create()), "Detaliu", variant, default);
    }
    [Fact]
    public async Task Files_follow_publication_state_and_gallery_changes_persist()
    {
        var (product, variant) = await Create();
        var first = await Upload(product);
        var second = await Upload(product, variant);
        await using var scope = fixture.Services.CreateAsyncScope();
        var s = scope.ServiceProvider;
        var read = s.GetRequiredService<GetImageHandler>();
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => read.Handle(product, first, "full", false, default))).Status);
        await using (var preview = await read.Handle(product, first, "thumbnail", true, default)) Assert.True(preview.Length > 0);
        await s.GetRequiredService<ArrangeImagesHandler>().Handle(product, new([second, first], second), default);
        await s.GetRequiredService<UpdateImageHandler>().Handle(product, first, new("Vedere frontală", variant), default);
        var draft = (await s.GetRequiredService<ICatalogReader>().Get(product, true, default))!;
        Assert.Equal(second, draft.Images![0].Id);
        Assert.True(draft.Images[0].IsPrimary);
        Assert.Equal("Vedere frontală", draft.Images[1].AltText);
        await s.GetRequiredService<PublishStoreProductHandler>().Handle(product, default);
        await using (var published = await read.Handle(product, first, "full", false, default)) Assert.True(published.Length > 0);
        await s.GetRequiredService<DeleteImageHandler>().Handle(product, second, default);
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => read.Handle(product, second, "full", true, default))).Status);
        Assert.Null(await s.GetRequiredService<IImageStorage>().Open(second, false, default));
        Assert.True(Assert.Single((await s.GetRequiredService<ICatalogReader>().Get(product, true, default))!.Images!).IsPrimary);
        await s.GetRequiredService<ArchiveProductHandler>().Handle(product, default);
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => read.Handle(product, first, "full", false, default))).Status);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => s.GetRequiredService<DeleteImageHandler>().Handle(product, first, default))).Status);
    }
    [Fact]
    public async Task Parallel_uploads_keep_one_primary_and_foreign_image_ids_are_rejected()
    {
        var (product, _) = await Create();
        var ids = await Task.WhenAll(Upload(product), Upload(product));
        var (other, variant) = await Create();
        await using var scope = fixture.Services.CreateAsyncScope();
        var s = scope.ServiceProvider;
        var dto = (await s.GetRequiredService<ICatalogReader>().Get(product, true, default))!;
        Assert.Equal(2, dto.Images!.Count);
        Assert.Single(dto.Images, x => x.IsPrimary);
        Assert.Equal(new[] { 0, 1 }, dto.Images.Select(x => x.SortOrder));
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => s.GetRequiredService<DeleteImageHandler>().Handle(other, ids[0], default))).Status);
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => s.GetRequiredService<UpdateImageHandler>().Handle(product, ids[0], new("Wrong SKU", variant), default))).Status);
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => s.GetRequiredService<GetImageHandler>().Handle(other, ids[0], "full", true, default))).Status);
    }
}
