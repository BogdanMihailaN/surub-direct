using SurubDirect.Catalog.Contracts;
using SurubDirect.Catalog.Contracts.Pricing;

namespace SurubDirect.Catalog.Application;
public record StoreProduct(ProductDto Product, IReadOnlyList<PriceDto> Prices, IReadOnlyList<StockSummary> Stock);
