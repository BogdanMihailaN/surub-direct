using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Domain;
namespace SurubDirect.Purchasing.Application;
public sealed class ListSuppliersHandler(ISupplierRepository repository)
{
    public Task<SurubDirect.Purchasing.Contracts.SupplierPage> Handle(string? search, bool? active, int page, CancellationToken ct)
    {
        if (page < 1 || page > 100000 || search?.Length > 200) throw new BusinessException("Filtre de căutare invalide.");
        return repository.List(search?.Trim(), active, page, ct);
    }
}
