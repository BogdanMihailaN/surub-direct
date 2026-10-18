using System.Net;
using SurubDirect.BuildingBlocks;
using SurubDirect.Catalog.Api.Clients;
using SurubDirect.Inventory.Api.Clients;
using Xunit;

namespace SurubDirect.Tests;
public sealed class ServiceClientTests
{
    [Fact]
    public async Task Missing_variant_is_distinct_from_catalog_outage()
    {
        var missing = new CatalogClient(new StubHttpClientFactory(_ => new(HttpStatusCode.NotFound)));
        Assert.False(await missing.VariantExists(Guid.NewGuid(), default));
        var unavailable = new CatalogClient(new StubHttpClientFactory(_ => new(HttpStatusCode.ServiceUnavailable)));
        var error = await Assert.ThrowsAsync<BusinessException>(() => unavailable.VariantExists(Guid.NewGuid(), default));
        Assert.Equal(503, error.Status);
    }

    [Fact]
    public async Task Inventory_outage_is_never_reported_as_zero_stock()
    {
        var client = new InventoryClient(new StubHttpClientFactory(_ => throw new HttpRequestException("Unavailable")));
        var error = await Assert.ThrowsAsync<BusinessException>(() => client.GetStock([Guid.NewGuid()], default));
        Assert.Equal(503, error.Status);
    }

    [Fact]
    public async Task Invalid_inventory_response_is_not_an_empty_success()
    {
        var client = new InventoryClient(new StubHttpClientFactory(_ => new(HttpStatusCode.OK) { Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json") }));
        var error = await Assert.ThrowsAsync<BusinessException>(() => client.GetStock([Guid.NewGuid()], default));
        Assert.Equal(503, error.Status);
    }
}
