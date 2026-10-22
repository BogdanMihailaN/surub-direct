using System.Net;
using System.Net.Http.Json;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Domain;

namespace SurubDirect.Ordering.Infrastructure;
public sealed class InventoryReservationsClient(IHttpClientFactory clients) : IReservationInventory
{
    public Task<ReservationObservation> Consume(ReservationPlan plan, CancellationToken ct) => Send(plan, "consume", ct);
    public Task<ReservationObservation> Reserve(ReservationPlan plan, CancellationToken ct) => Send(plan, "reserve", ct);
    public Task<ReservationObservation> Release(ReservationPlan plan, CancellationToken ct) => Send(plan, "release", ct);
    private async Task<ReservationObservation> Send(ReservationPlan p, string operation, CancellationToken ct)
    {
        object payload = operation != "reserve" ? new
        {
            p.OrderId
        }

        : new
        {
            p.Id,
            p.OrderId,
            DurationSeconds = 900,
            p.Lines
        };
        var path = operation == "reserve" ? "/internal/reservations" : $"/internal/reservations/{p.Id}/{operation}";
        using var response = await clients.CreateClient("inventory-reservations").PostAsJsonAsync(path, payload, ct);
        if (operation == "reserve" && response.StatusCode == HttpStatusCode.Conflict)
            throw new ReservationRejectedException();
        response.EnsureSuccessStatusCode();
        var value = await response.Content.ReadFromJsonAsync<InventoryReservationResponse>(ct);
        if (value is null || value.Id != p.Id || value.OrderId != p.OrderId || value.ExpiresAt == default || value.Status is not ("Active" or "Released" or "Expired" or "Consumed") || value.Lines is null || value.Lines.Any(x => x is null) || (operation != "reserve" && value.Status == "Active") || !(value.Status == "Released" && value.Lines.Length == 0) && !value.Lines.OrderBy(x => x.VariantId).Select(x => new ReservationItem(x.VariantId, x.Quantity)).SequenceEqual(p.Lines))
            throw new HttpRequestException("Invalid reservation response.");
        return new(value.Status, value.ExpiresAt);
    }
}
