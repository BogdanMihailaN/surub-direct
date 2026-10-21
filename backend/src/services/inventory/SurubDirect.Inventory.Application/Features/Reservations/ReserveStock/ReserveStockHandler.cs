using SurubDirect.Inventory.Contracts;
using SurubDirect.Inventory.Domain;

namespace SurubDirect.Inventory.Application;
public sealed class ReserveStockHandler(IStockReservations repository, TimeProvider clock)
{
    public async Task<ReservationDto> Handle(ReserveStock command, CancellationToken ct) => ReservationProjection.Map(await repository.Reserve(StockReservation.Create(command.Id, command.OrderId, command.DurationSeconds, command.Lines, clock.GetUtcNow()), ct));
}
