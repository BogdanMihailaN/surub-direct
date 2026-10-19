namespace SurubDirect.Catalog.Contracts;
public interface IPurchasingVariantReader
{
    Task<PurchasingVariantDto?> Find(Guid id, CancellationToken ct);
    Task<IReadOnlyList<PurchasingVariantDto>> Search(string search, CancellationToken ct);
}
