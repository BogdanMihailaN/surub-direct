using SurubDirect.Catalog.Contracts;
using SurubDirect.Pricing.Contracts;

namespace SurubDirect.Store.Application;
public record StoreProduct(ProductDto Product, IReadOnlyList<PriceDto> Prices, IReadOnlyList<StockSummary> Stock);
