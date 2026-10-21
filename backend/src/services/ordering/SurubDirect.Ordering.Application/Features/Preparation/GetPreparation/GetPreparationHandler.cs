using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Application;
public sealed class GetPreparationHandler(IPreparedCarts repository)
{
    public Task<PreparedCartDto?> Handle(string owner, CancellationToken ct) => repository.Read(owner, ct);
}
