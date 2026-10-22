using SurubDirect.Inventory.Contracts;

namespace SurubDirect.Inventory.Application;
public sealed class RestoreStockHandler(IStockReservations repository)
{
    public async Task<ReservationDto> Handle(Guid id, RestoreStock command, CancellationToken ct) => ReservationProjection.Map(await repository.Restore(id, command.OrderId, ct));
}
