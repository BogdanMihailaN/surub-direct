using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Application;
public interface IMessageFailureMonitor
{
    Task<MessageFailureSummary> Read(CancellationToken ct);
}
