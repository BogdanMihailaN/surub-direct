using SurubDirect.Catalog.Domain;
using SurubDirect.Pricing.Domain;
namespace SurubDirect.Store.Application.Imports;
public sealed record ImportBatch(ImportPreview Preview, IReadOnlyList<Product> Products, IReadOnlyList<OfferPrice> Prices);
