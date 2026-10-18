using SurubDirect.Catalog.Contracts;
using SurubDirect.Inventory.Application;

namespace SurubDirect.Tests;
// Test-only bridge. Production Inventory resolves this port through HTTP.
public sealed class TestVariantLookup(ICatalogReader catalog) : IVariantLookup
{
    public Task<bool> VariantExists(Guid id, CancellationToken ct) => catalog.VariantExists(id, ct);
}
