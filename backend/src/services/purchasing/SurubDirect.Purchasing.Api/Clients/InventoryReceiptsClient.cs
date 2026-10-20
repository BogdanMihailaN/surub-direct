using System.Text.Json;
using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Application;
namespace SurubDirect.Purchasing.Api.Clients;
public sealed class InventoryReceiptsClient(IHttpClientFactory clients, IHttpContextAccessor context) : IInventoryReceipts
{
    public async Task Receive(Guid requestId, Guid variantId, int quantity, string reference, CancellationToken ct)
    {
        var incoming = context.HttpContext?.Request ?? throw new InvalidOperationException("An administrative request is required.");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/variants/{variantId}/receipts");
        request.Content = JsonContent.Create(new { requestId, quantity, reference });
        request.Headers.TryAddWithoutValidation("Cookie", incoming.Headers.Cookie.ToString());
        request.Headers.TryAddWithoutValidation("X-CSRF-TOKEN", incoming.Headers["X-CSRF-TOKEN"].ToString());
        try
        {
            using var response = await clients.CreateClient("inventory-receipts").SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            if (!body.TryGetProperty("created", out var created) || created.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) throw Unavailable();
        }
        catch (HttpRequestException) { throw Unavailable(); }
        catch (JsonException) { throw Unavailable(); }
        catch (InvalidOperationException) { throw Unavailable(); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw Unavailable(); }
    }
    private static BusinessException Unavailable() => new("Stocul nu a putut fi confirmat. Recepția este salvată în așteptare; reîncarcă și reia aceeași recepție.", 503);
}
