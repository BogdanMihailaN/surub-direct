namespace SurubDirect.Inventory.Application;
public sealed class ExpireReservationsHandler(IStockReservations repository)
{
    public Task<int> Handle(CancellationToken ct) => repository.Expire(100, ct);
}
