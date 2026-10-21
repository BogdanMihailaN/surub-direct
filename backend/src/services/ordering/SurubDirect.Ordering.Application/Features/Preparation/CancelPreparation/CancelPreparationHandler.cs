namespace SurubDirect.Ordering.Application;
public sealed class CancelPreparationHandler(IPreparedCarts repository)
{
    public Task Handle(string owner, Guid id, CancellationToken ct) => repository.Cancel(owner, id, ct);
}
