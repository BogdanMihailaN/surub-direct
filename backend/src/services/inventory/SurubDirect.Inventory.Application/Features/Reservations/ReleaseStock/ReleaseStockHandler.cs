using SurubDirect.Inventory.Contracts;

namespace SurubDirect.Inventory.Application;
public sealed class ReleaseStockHandler(IStockReservations repository)
{
    public async Task<ReservationDto> Handle(Guid id, ReleaseStock command, CancellationToken ct) => ReservationProjection.Map(await repository.Release(id, command.OrderId, ct));
}
