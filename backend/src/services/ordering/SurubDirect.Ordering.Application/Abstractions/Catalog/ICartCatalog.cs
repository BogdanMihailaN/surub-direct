namespace SurubDirect.Ordering.Application;

public interface ICartCatalog { Task<IReadOnlyList<CartOfferSnapshot>?> Read(string slug, CancellationToken ct); }
