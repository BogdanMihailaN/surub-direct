using System.Net;
using Microsoft.AspNetCore.Http;
using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Api.Clients;
using Xunit;
namespace SurubDirect.Tests;
public sealed class InventoryReceiptsClientTests
{
    [Theory]
    [InlineData("{\"created\":true}")]
    [InlineData("{\"created\":false}")]
    public async Task Original_and_duplicate_receipts_forward_session_and_csrf(string body)
    {
        var context = new DefaultHttpContext(); context.Request.Headers.Cookie = "session=test"; context.Request.Headers["X-CSRF-TOKEN"] = "csrf-test";
        var client = new InventoryReceiptsClient(new StubHttpClientFactory(request => {
            Assert.Equal("session=test", Assert.Single(request.Headers.GetValues("Cookie")));
            Assert.Equal("csrf-test", Assert.Single(request.Headers.GetValues("X-CSRF-TOKEN")));
            Assert.Equal(HttpMethod.Post, request.Method);
            return new(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };
        }), new HttpContextAccessor { HttpContext = context });
        await client.Receive(Guid.NewGuid(), Guid.NewGuid(), 10, "Aviz", default);
    }
    [Theory]
    [InlineData("{}", 200)]
    [InlineData("null", 200)]
    [InlineData("{\"created\":\"true\"}", 200)]
    [InlineData("error", 503)]
    [InlineData("error", 401)]
    public async Task Unconfirmed_results_leave_the_receipt_pending(string body, int status)
    {
        var client = new InventoryReceiptsClient(new StubHttpClientFactory(_ => new((HttpStatusCode)status) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") }), new HttpContextAccessor { HttpContext = new DefaultHttpContext() });
        var error = await Assert.ThrowsAsync<BusinessException>(() => client.Receive(Guid.NewGuid(), Guid.NewGuid(), 10, "Aviz", default));
        Assert.Equal(503, error.Status);
    }
}
