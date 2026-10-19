using System.Net;
using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Api.Clients;
using Xunit;
namespace SurubDirect.Tests;
public sealed class PurchasingCatalogClientTests
{
    [Fact]
    public async Task Missing_variant_is_distinct_from_an_unavailable_catalog()
    {
        var missing = new CatalogVariantsClient(new StubHttpClientFactory(_ => new(HttpStatusCode.NotFound)));
        Assert.Null(await missing.Find(Guid.NewGuid(), default));
        var unavailable = new CatalogVariantsClient(new StubHttpClientFactory(_ => new(HttpStatusCode.ServiceUnavailable)));
        Assert.Equal(503, (await Assert.ThrowsAsync<BusinessException>(() => unavailable.Find(Guid.NewGuid(), default))).Status);
        Assert.Equal(503, (await Assert.ThrowsAsync<BusinessException>(() => unavailable.Search("SKU", default))).Status);
    }
    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("<html>error</html>")]
    public async Task Invalid_contracts_are_reported_as_dependency_failures(string json)
    {
        var client = new CatalogVariantsClient(new StubHttpClientFactory(_ => new(HttpStatusCode.OK) { Content = new StringContent(json,System.Text.Encoding.UTF8,"application/json") }));
        Assert.Equal(503, (await Assert.ThrowsAsync<BusinessException>(() => client.Find(Guid.NewGuid(), default))).Status);
    }
}
