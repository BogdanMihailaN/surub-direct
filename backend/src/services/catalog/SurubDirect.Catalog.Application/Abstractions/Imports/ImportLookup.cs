using SurubDirect.Catalog.Domain;
namespace SurubDirect.Catalog.Application.Imports;
public sealed record ImportLookup(IReadOnlyList<Category> Categories, IReadOnlySet<string> Slugs,
    IReadOnlySet<string> Skus, IReadOnlySet<string> OfferCodes);
