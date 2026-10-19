using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Domain;
namespace SurubDirect.Purchasing.Application;
public sealed class GetSupplierHandler(ISupplierRepository repository)
{
    public async Task<SurubDirect.Purchasing.Contracts.SupplierDto> Handle(Guid id, CancellationToken ct) =>
        await repository.Get(id, ct) ?? throw new BusinessException("Furnizorul nu există.", 404);
}
