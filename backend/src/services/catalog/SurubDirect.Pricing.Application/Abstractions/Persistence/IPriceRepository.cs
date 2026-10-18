using SurubDirect.Pricing.Domain;

namespace SurubDirect.Pricing.Application;
public interface IPriceRepository
{
    Task<OfferPrice?> Get(Guid id, CancellationToken ct);
    Task Save(OfferPrice price, CancellationToken ct);
}
