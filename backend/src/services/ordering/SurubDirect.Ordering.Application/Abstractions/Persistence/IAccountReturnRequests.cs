namespace SurubDirect.Ordering.Application;
public interface IAccountReturnRequests
{
    Task Request(string accountId, Guid orderId, Guid operationId, string reason, CancellationToken ct);
}
