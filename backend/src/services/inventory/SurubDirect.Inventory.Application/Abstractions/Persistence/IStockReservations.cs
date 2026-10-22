using SurubDirect.Inventory.Domain;

namespace SurubDirect.Inventory.Application;
public interface IStockReservations
{
    Task<StockReservation> Reserve(StockReservation reservation, CancellationToken ct);
    Task<StockReservation> Consume(Guid id, Guid orderId, CancellationToken ct);
    Task<StockReservation> Release(Guid id, Guid orderId, CancellationToken ct);
    Task<StockReservation?> Read(Guid id, CancellationToken ct);
    Task<int> Expire(int limit, CancellationToken ct);
}
