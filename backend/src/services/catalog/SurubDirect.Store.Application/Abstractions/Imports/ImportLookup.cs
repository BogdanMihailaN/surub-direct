using SurubDirect.Catalog.Domain;
namespace SurubDirect.Store.Application.Imports;
public sealed record ImportLookup(IReadOnlyList<Category> Categories, IReadOnlySet<string> Slugs,
    IReadOnlySet<string> Skus, IReadOnlySet<string> OfferCodes);
