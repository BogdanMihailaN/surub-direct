using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Domain;
namespace SurubDirect.Purchasing.Application;
public sealed class GetPurchaseOrderHandler(IPurchaseOrderRepository repository)
{ public async Task<SurubDirect.Purchasing.Contracts.PurchaseOrderDto> Handle(Guid id, CancellationToken ct) => await repository.Get(id, ct) ?? throw new BusinessException("Comanda nu există.", 404); }
