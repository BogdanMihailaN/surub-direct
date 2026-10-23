using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Application;
public interface IMessagingAdministration
{
    Task<MessagingStatusDto> Read(int page, CancellationToken ct);
    Task<FailedMessageDto> Replay(Guid id, RequestMessageReplay request, string actor, CancellationToken ct);
}
