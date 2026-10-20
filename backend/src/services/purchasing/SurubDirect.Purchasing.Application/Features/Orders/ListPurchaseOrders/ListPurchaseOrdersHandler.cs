using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Domain;
namespace SurubDirect.Purchasing.Application;
public sealed class ListPurchaseOrdersHandler(IPurchaseOrderRepository repository)
{
    public Task<SurubDirect.Purchasing.Contracts.PurchaseOrderPage> Handle(string? search, string? status, int page, CancellationToken ct)
    {
        if (page < 1 || page > 100000 || search?.Length > 200) throw new BusinessException("Filtre invalide.");
        PurchaseOrderStatus? filter = null;
        if (!string.IsNullOrEmpty(status))
        {
            if (!Enum.TryParse<PurchaseOrderStatus>(status, out var parsed) || !Enum.IsDefined(parsed)) throw new BusinessException("Stare invalidă.");
            filter = parsed;
        }
        return repository.List(search?.Trim(), filter, page, ct);
    }
}
