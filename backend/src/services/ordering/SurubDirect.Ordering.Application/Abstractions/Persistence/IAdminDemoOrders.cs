using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Application;
public interface IAdminDemoOrders
{
    Task<AdminOrderPageDto> Read(string? status, long? sequence, int page, CancellationToken ct, string? returnStatus = null, string? paymentStatus = null);
    Task<AdminOrderDetailDto?> Find(Guid id, CancellationToken ct);
}
