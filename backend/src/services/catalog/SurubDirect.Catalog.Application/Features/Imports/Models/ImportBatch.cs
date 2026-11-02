using SurubDirect.Catalog.Domain;
using SurubDirect.Catalog.Domain.Pricing;
namespace SurubDirect.Catalog.Application.Imports;
public sealed record ImportBatch(ImportPreview Preview, IReadOnlyList<Product> Products, IReadOnlyList<OfferPrice> Prices);
