using SurubDirect.Ordering.Domain;
using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Application;
public interface IPreparedCarts
{
    Task<PreparedCartDto?> Read(string owner, CancellationToken ct, string? accountId = null);
    Task<PreparedCartDto> Save(string owner, PreparedCart snapshot, ReservationPlan plan, CancellationToken ct, string? accountId = null);
    Task Cancel(string owner, Guid preparedId, CancellationToken ct, string? accountId = null);
}
