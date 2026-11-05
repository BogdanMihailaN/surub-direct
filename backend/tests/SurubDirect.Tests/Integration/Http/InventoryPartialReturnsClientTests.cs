using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SurubDirect.Ordering.Domain;
using SurubDirect.Ordering.Infrastructure;
using Xunit;
namespace SurubDirect.Tests;
public sealed class InventoryPartialReturnsClientTests
{
    private static PartialReturnRequest Request()
    {
        var (order, _) = ReturnSamples.Create();
        var line = order.Lines[0];
        order.Lines.Add(line with { OfferId = Guid.NewGuid(), UnitsPerPack = 5 });
        return PartialReturnRequest.Create(Guid.NewGuid(), order, order.Lines.Select(x => new PartialReturnSelection(x.OfferId, 1)), "Retur produse", "admin", DateTimeOffset.UtcNow);
    }
    [Fact]
    public async Task Aggregates_same_sku_offers_and_reuses_request_id_as_inventory_receipt_key()
    {
        var r = Request(); var now = DateTimeOffset.UtcNow;
        var client = new InventoryPartialReturnsClient(new StubHttpClientFactory(message =>
        {
            Assert.Equal($"/internal/reservations/{r.OrderId}/return-receipts/{r.Id}", message.RequestUri!.AbsolutePath);
            var payload = JsonDocument.Parse(message.Content!.ReadAsStringAsync().GetAwaiter().GetResult());
            var line = Assert.Single(payload.RootElement.GetProperty("lines").EnumerateArray());
            Assert.Equal(15, line.GetProperty("quantity").GetInt32());
            return new(HttpStatusCode.OK) { Content = JsonContent.Create(new InventoryReturnReceiptResponse(r.Id, r.OrderId, r.OrderId, now, [new(r.Lines[0].VariantId, 15)])) };
        }));
        Assert.Equal(now, await client.Receive(r, default));
    }
    [Theory]
    [InlineData("receipt")]
    [InlineData("order")]
    [InlineData("reservation")]
    [InlineData("quantity")]
    [InlineData("duplicate")]
    [InlineData("null")]
    [InlineData("date")]
    public async Task Invalid_confirmation_is_never_accepted(string kind)
    {
        var r = Request();
        var value = new InventoryReturnReceiptResponse(r.Id, r.OrderId, r.OrderId, DateTimeOffset.UtcNow, [new(r.Lines[0].VariantId, 15)]);
        value = kind switch {
            "receipt" => value with { Id = Guid.NewGuid() }, "order" => value with { OrderId = Guid.NewGuid() },
            "reservation" => value with { ReservationId = Guid.NewGuid() }, "quantity" => value with { Lines = [new(r.Lines[0].VariantId, 16)] },
            "duplicate" => value with { Lines = [new(r.Lines[0].VariantId, 15), new(r.Lines[0].VariantId, 15)] },
            "null" => value with { Lines = null }, _ => value with { CreatedAt = default }
        };
        var client = new InventoryPartialReturnsClient(new StubHttpClientFactory(_ => new(HttpStatusCode.OK) { Content = JsonContent.Create(value) }));
        await Assert.ThrowsAsync<HttpRequestException>(() => client.Receive(r, default));
    }
    [Theory]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Dependency_failures_remain_unconfirmed(HttpStatusCode status)
    {
        var client = new InventoryPartialReturnsClient(new StubHttpClientFactory(_ => new(status)));
        await Assert.ThrowsAsync<HttpRequestException>(() => client.Receive(Request(), default));
    }
}
