namespace SurubDirect.Inventory.Application;
public interface IVariantLookup
{
    Task<bool> VariantExists(Guid id, CancellationToken ct);
}
