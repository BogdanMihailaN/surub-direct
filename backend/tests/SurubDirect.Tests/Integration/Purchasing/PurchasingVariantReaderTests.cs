using Microsoft.Extensions.DependencyInjection;
using SurubDirect.Catalog.Application;
using SurubDirect.Catalog.Contracts;
using Xunit;
namespace SurubDirect.Tests;
public sealed class PurchasingVariantReaderTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task Purchasing_directory_includes_drafts_but_excludes_archived_products()
    {
        await using var scope=fixture.Services.CreateAsyncScope(); var s=scope.ServiceProvider;
        var sku="DIRECTORY-"+Guid.NewGuid().ToString("N");
        var product=await s.GetRequiredService<CreateProductHandler>().Handle(new("Product",sku.ToLowerInvariant(),"Test","Description"),default);
        var variant=await s.GetRequiredService<AddVariantHandler>().Handle(product,new(sku,"M6",6,20,"Steel","Zinc"),default);
        var reader=s.GetRequiredService<IPurchasingVariantReader>();
        Assert.Equal(variant,Assert.Single(await reader.Search(sku,default)).Id);
        Assert.NotNull(await reader.Find(variant,default));
        await s.GetRequiredService<ArchiveProductHandler>().Handle(product,default);
        Assert.Empty(await reader.Search(sku,default)); Assert.Null(await reader.Find(variant,default));
    }
}
