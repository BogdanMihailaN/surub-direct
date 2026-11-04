using SurubDirect.BuildingBlocks;
namespace SurubDirect.Ordering.Application;
public sealed class RequestAccountReturnHandler(IAccountReturnRequests requests)
{
    public Task Handle(string accountId, Guid id, Guid operationId, string? reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(accountId) || accountId.Length > 180 || id == Guid.Empty || operationId == Guid.Empty
            || string.IsNullOrWhiteSpace(reason) || reason.Trim().Length is < 3 or > 500)
            throw new BusinessException("Comanda, operația și motivul de 3–500 caractere sunt obligatorii.");
        return requests.Request(accountId, id, operationId, reason.Trim(), ct);
    }
}
