using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Application;
public sealed class GetMessagingStatusHandler(IMessagingAdministration administration)
{
    public Task<MessagingStatusDto> Handle(int page, CancellationToken ct)
    {
        if (page is < 1 or > 100000)
            throw new BusinessException("Pagina nu este validă.");
        return administration.Read(page, ct);
    }
}
