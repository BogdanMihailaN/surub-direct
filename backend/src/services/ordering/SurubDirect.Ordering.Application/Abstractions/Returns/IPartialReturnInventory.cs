using SurubDirect.Ordering.Domain;
namespace SurubDirect.Ordering.Application;
public interface IPartialReturnInventory
{
    Task<DateTimeOffset> Receive(PartialReturnRequest request, CancellationToken ct);
}
