using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Application;
public interface IBrokerMonitor
{
    Task<BrokerStatusDto> Read(CancellationToken ct);
}
