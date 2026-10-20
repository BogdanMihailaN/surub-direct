using System.Text.Json;
using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Application;
namespace SurubDirect.Purchasing.Api.Clients;

public sealed class InventoryCorrectionsClient(IHttpClientFactory clients, IHttpContextAccessor context) : IInventoryCorrections
{
    public async Task Apply(Guid requestId, Guid receiptId, Guid variantId, int quantity, string reference, string reason, CancellationToken ct)
    {
        var incoming = context.HttpContext?.Request ?? throw new InvalidOperationException("An administrative request is required.");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/internal/receipt-corrections");
        request.Content = JsonContent.Create(new { requestId, receiptId, variantId, quantity, reference, reason });
        request.Headers.TryAddWithoutValidation("Cookie", incoming.Headers.Cookie.ToString());
        request.Headers.TryAddWithoutValidation("X-CSRF-TOKEN", incoming.Headers["X-CSRF-TOKEN"].ToString());
        try
        {
            using var response = await clients.CreateClient("inventory-corrections").SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            if (!body.TryGetProperty("created", out var created) || created.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) throw Unavailable();
        }
        catch (HttpRequestException) { throw Unavailable(); }
        catch (JsonException) { throw Unavailable(); }
        catch (InvalidOperationException) { throw Unavailable(); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw Unavailable(); }
    }
    private static BusinessException Unavailable() => new("Corecția este în așteptare. Verifică stocul disponibil și reia aceeași corecție; cantitatea comenzii nu a fost încă eliberată.", 503);
}
