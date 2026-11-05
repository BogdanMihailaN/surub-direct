using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Domain;
namespace SurubDirect.Tests;
public sealed class TestPartialReturnInventory(Func<PartialReturnRequest, CancellationToken, Task<DateTimeOffset>> receive) : IPartialReturnInventory
{
    public Task<DateTimeOffset> Receive(PartialReturnRequest request, CancellationToken ct) => receive(request, ct);
}
