using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Contracts;
using SurubDirect.Ordering.Domain;
namespace SurubDirect.Ordering.Application;
internal sealed class CartPreparation(IPreparedCarts prepared, QuoteCartHandler quote)
{
    public async Task<PreparedCartDto> Handle(ShoppingCart cart, PrepareCart command, CancellationToken ct)
    {
        if (command.RequestId == Guid.Empty)
            throw new BusinessException("Identificator pregătire invalid.");
        cart.EnsureActive(DateTimeOffset.UtcNow);
        if (cart.Id != command.CartId)
            throw new BusinessException("Coșul s-a modificat.", 409);
        if (cart.PreparedId == command.RequestId)
            return await prepared.Read(cart.OwnerHash, ct, cart.AccountId) ?? throw new BusinessException("Pregătire indisponibilă.", 409);
        if (cart.Version != command.Version || cart.PreparedId is not null)
            throw new BusinessException("Reîncarcă coșul înainte de pregătire.", 409);
        if (cart.Customer is null || cart.Lines.Count == 0)
            throw new BusinessException("Adaugă produse și salvează datele clientului.");
        var current = await quote.Handle(new(cart.Lines), ct);
        if (current.GrossTotal is null || current.NetTotal is null || current.Currency is null || current.Lines.Any(x => x.Issue is not null || x.VariantId is null || x.UnitNet is null || x.UnitGross is null))
            throw new BusinessException("Produsele, prețurile sau cantitățile nu mai permit pregătirea. Recalculează coșul.", 409);
        var grouped = current.Lines.GroupBy(x => x.VariantId!.Value).Select(g => new { Id = g.Key, Quantity = g.Sum(x => x.BaseUnits) }).ToArray();
        if (grouped.Any(x => x.Quantity > int.MaxValue))
            throw new BusinessException("Cantitatea cumulată pe SKU depășește limita.");
        var snapshot = PreparedCart.Create(command.RequestId, cart, current.Lines.Select(x => new PreparedCartLine(x.OfferId, x.VariantId!.Value, x.ProductName, x.Sku, x.OfferLabel, x.Packs, x.UnitsPerPack, x.UnitNet!.Value, x.UnitGross!.Value, x.NetTotal!.Value, x.GrossTotal!.Value)).ToArray(), current.Currency, current.CheckedAt);
        var plan = ReservationPlan.Create(command.RequestId, command.RequestId, grouped.Select(x => new ReservationItem(x.Id, (int)x.Quantity)).ToArray(), DateTimeOffset.UtcNow);
        return await prepared.Save(cart.OwnerHash, snapshot, plan, ct, cart.AccountId);
    }
}
