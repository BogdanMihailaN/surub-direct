using SurubDirect.BuildingBlocks;
namespace SurubDirect.Ordering.Application;
public sealed class LinkAccountOrderHandler(IAccountOrders orders)
{
    public Task Handle(string accountId, string owner, Guid id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(accountId) || accountId.Length > 450 || string.IsNullOrEmpty(owner) || owner.Length != 64 || id == Guid.Empty)
            throw new BusinessException("Datele asocierii nu sunt valide.");
        return orders.Link(accountId, owner, id, ct);
    }
}
