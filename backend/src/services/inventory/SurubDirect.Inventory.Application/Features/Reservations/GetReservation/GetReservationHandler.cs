using SurubDirect.BuildingBlocks;
using SurubDirect.Inventory.Contracts;

namespace SurubDirect.Inventory.Application;
public sealed class GetReservationHandler(IStockReservations repository)
{
    public async Task<ReservationDto> Handle(Guid id, CancellationToken ct) => ReservationProjection.Map(await repository.Read(id, ct) ?? throw new BusinessException("Rezervarea nu există.", 404));
}
