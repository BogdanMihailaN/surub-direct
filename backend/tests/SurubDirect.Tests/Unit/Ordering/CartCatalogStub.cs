using SurubDirect.Ordering.Application;
namespace SurubDirect.Tests;

public sealed class CartCatalogStub(IReadOnlyList<CartOfferSnapshot>? offers) : ICartCatalog
{
    public int Calls { get; private set; }
    public Task<IReadOnlyList<CartOfferSnapshot>?> Read(string slug, CancellationToken ct) { Calls++; return Task.FromResult(offers); }
}
