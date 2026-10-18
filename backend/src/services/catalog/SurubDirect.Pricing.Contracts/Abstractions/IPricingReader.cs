namespace SurubDirect.Pricing.Contracts;
public interface IPricingReader
{
    Task<IReadOnlyList<PriceDto>> GetPrices(IReadOnlyList<Guid> ids, CancellationToken ct);
}
