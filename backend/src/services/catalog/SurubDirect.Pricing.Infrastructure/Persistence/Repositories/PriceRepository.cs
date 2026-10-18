using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.Pricing.Application;
using SurubDirect.Pricing.Contracts;
using SurubDirect.Pricing.Domain;

namespace SurubDirect.Pricing.Infrastructure;
internal sealed class PriceRepository(PricingDbContext db) : IPriceRepository, IPricingReader
{
    public Task<OfferPrice?> Get(Guid id, CancellationToken ct) => db.Prices.SingleOrDefaultAsync(x => x.OfferId == id, ct);
    public async Task Save(OfferPrice p, CancellationToken ct)
    {
        if (db.Entry(p).State == EntityState.Detached)
            db.Prices.Add(p);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<PriceDto>> GetPrices(IReadOnlyList<Guid> ids, CancellationToken ct) => (await db.Prices.AsNoTracking().Where(x => ids.Contains(x.OfferId)).ToListAsync(ct)).Select(x => new PriceDto(x.OfferId, x.NetAmount, x.TaxRate, x.GrossAmount, x.Currency)).ToArray();
}
