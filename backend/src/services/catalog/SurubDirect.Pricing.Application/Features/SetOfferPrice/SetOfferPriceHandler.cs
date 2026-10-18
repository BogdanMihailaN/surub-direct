using SurubDirect.BuildingBlocks;
using SurubDirect.Catalog.Contracts;
using SurubDirect.Pricing.Domain;

namespace SurubDirect.Pricing.Application;
public sealed class SetOfferPriceHandler(IPriceRepository repository, ICatalogReader catalog)
{
    public async Task Handle(Guid id, SetPrice c, CancellationToken ct)
    {
        if (!await catalog.OfferExists(id, ct))
            throw new BusinessException("Oferta nu există.", 404);
        var price = await repository.Get(id, ct);
        if (price == null)
            price = OfferPrice.Create(id, c.NetAmount, c.TaxRate);
        else
            price.Change(c.NetAmount, c.TaxRate);
        await repository.Save(price, ct);
    }
}
