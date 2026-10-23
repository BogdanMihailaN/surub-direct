using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Application;
public sealed class ReplayMessageHandler(IMessagingAdministration administration)
{
    public Task<FailedMessageDto> Handle(Guid id, RequestMessageReplay request, string actor, CancellationToken ct)
    {
        if (request.RequestId == Guid.Empty || string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length is < 3 or > 500 || string.IsNullOrWhiteSpace(actor) || actor.Length > 200)
            throw new BusinessException("Completează un motiv de 3–500 caractere și identificatorul cererii.");
        return administration.Replay(id, request with { Reason = request.Reason.Trim() }, actor, ct);
    }
}
