using System.Net.Http.Json;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Domain;
namespace SurubDirect.Ordering.Infrastructure;
public sealed class InventoryPartialReturnsClient(IHttpClientFactory clients) : IPartialReturnInventory
{
    public async Task<DateTimeOffset> Receive(PartialReturnRequest request, CancellationToken ct)
    {
        var lines = request.Lines.GroupBy(x => x.VariantId).Select(g => new InventoryReturnLine(g.Key, checked((int)g.Sum(x => (long)x.Quantity)))).OrderBy(x => x.VariantId).ToArray();
        using var response = await clients.CreateClient("inventory-reservations").PostAsJsonAsync(
            $"/internal/reservations/{request.OrderId}/return-receipts/{request.Id}", new { request.OrderId, Lines = lines }, ct);
        response.EnsureSuccessStatusCode();
        var value = await response.Content.ReadFromJsonAsync<InventoryReturnReceiptResponse>(ct);
        if (value is null || value.Id != request.Id || value.OrderId != request.OrderId || value.ReservationId != request.OrderId
            || value.CreatedAt == default || value.Lines is null || value.Lines.Any(x => x is null)
            || !value.Lines.OrderBy(x => x.VariantId).SequenceEqual(lines))
            throw new HttpRequestException("Invalid return receipt response.");
        return value.CreatedAt;
    }
}
