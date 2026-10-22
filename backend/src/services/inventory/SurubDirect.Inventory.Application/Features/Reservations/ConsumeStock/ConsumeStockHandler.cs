using SurubDirect.Inventory.Contracts;

namespace SurubDirect.Inventory.Application;
public sealed class ConsumeStockHandler(IStockReservations repository)
{
    public async Task<ReservationDto> Handle(Guid id, ConsumeStock command, CancellationToken ct) => ReservationProjection.Map(await repository.Consume(id, command.OrderId, ct));
}
