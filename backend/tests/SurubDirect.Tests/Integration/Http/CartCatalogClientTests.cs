using System.Net;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Infrastructure;
using Xunit;
namespace SurubDirect.Tests;

public sealed class CartCatalogClientTests
{
    [Theory]
    [InlineData(503, "{}")]
    [InlineData(200, "{}")]
    [InlineData(200, "null")]
    [InlineData(200, "broken")]
    [InlineData(200, "{\"product\":{\"name\":\"Screw\",\"slug\":\"screw\",\"variants\":[null]},\"prices\":[],\"stock\":[]}")]
    public async Task Unavailable_or_malformed_catalog_is_never_zero_stock(int status, string body)
    {
        var client = new CartCatalogClient(new StubHttpClientFactory(_ => new((HttpStatusCode)status) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") }));
        Assert.Equal(503, (await Assert.ThrowsAsync<BusinessException>(() => client.Read("screw", default))).Status);
    }
    [Fact]
    public async Task Missing_product_is_distinct_from_dependency_failure()
    {
        var client = new CartCatalogClient(new StubHttpClientFactory(_ => new(HttpStatusCode.NotFound))); Assert.Null(await client.Read("screw", default));
    }
    [Fact]
    public async Task Missing_available_field_is_not_treated_as_zero_stock()
    {
        var variant = Guid.NewGuid(); var body = System.Text.Json.JsonSerializer.Serialize(new { product = new { name = "Screw", slug = "screw", variants = new[] { new { id = variant, sku = "SKU", offers = Array.Empty<object>() } } }, prices = Array.Empty<object>(), stock = new[] { new { variantId = variant } } });
        var client = new CartCatalogClient(new StubHttpClientFactory(_ => new(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") })); Assert.Equal(503, (await Assert.ThrowsAsync<BusinessException>(() => client.Read("screw", default))).Status);
    }
}
