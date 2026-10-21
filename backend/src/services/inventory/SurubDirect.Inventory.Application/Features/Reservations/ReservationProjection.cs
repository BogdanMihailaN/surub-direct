using SurubDirect.Inventory.Contracts;
using SurubDirect.Inventory.Domain;

namespace SurubDirect.Inventory.Application;
internal static class ReservationProjection
{
    public static ReservationDto Map(StockReservation r) => new(r.Id, r.OrderId, r.Status.ToString(), r.CreatedAt, r.ExpiresAt, r.EndedAt, r.Lines.Select(x => new ReservationLineDto(x.VariantId, x.Quantity)).ToArray());
}
