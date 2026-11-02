using SurubDirect.Catalog.Domain.Pricing;

namespace SurubDirect.Catalog.Application.Pricing;
public interface IPriceRepository
{
    Task<OfferPrice?> Get(Guid id, CancellationToken ct);
    Task Save(OfferPrice price, CancellationToken ct);
}
