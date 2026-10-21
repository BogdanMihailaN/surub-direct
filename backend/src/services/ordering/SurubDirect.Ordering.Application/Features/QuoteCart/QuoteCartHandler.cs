using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Contracts;
using SurubDirect.Ordering.Domain;

namespace SurubDirect.Ordering.Application;
public sealed class QuoteCartHandler(ICartCatalog catalog)
{
    public async Task<CartQuoteDto> Handle(QuoteCart command, CancellationToken ct)
    {
        var selections = command.Lines ?? throw new BusinessException("Lista coșului lipsește.");
        if (selections.Count > 50 || selections.Any(x => x is null) || selections.Select(x => x.OfferId).Distinct().Count() != selections.Count)
            throw new BusinessException("Coșul acceptă maximum 50 de ambalaje distincte.");
        foreach (var line in selections)
            line.Validate();
        var products = new Dictionary<string, IReadOnlyList<CartOfferSnapshot>?>();
        foreach (var slug in selections.Select(x => x.ProductSlug).Distinct())
            products[slug] = await catalog.Read(slug, ct);
        var offers = selections.Select(s => products[s.ProductSlug]?.SingleOrDefault(o => o.OfferId == s.OfferId)).ToArray();
        var requested = selections.Select((s, i) => (s, offer: offers[i])).Where(x => x.offer is not null && CartQuantity.IsValid(x.s.Packs, x.offer.UnitsPerPack, x.offer.MinPacks, x.offer.PackStep)).GroupBy(x => x.offer!.VariantId).ToDictionary(g => g.Key, g => g.Sum(x => (long)x.s.Packs * x.offer!.UnitsPerPack));
        var lines = new List<CartQuoteLineDto>();
        for (var i = 0; i < selections.Count; i++)
        {
            var s = selections[i];
            var o = offers[i];
            if (o is null)
            {
                lines.Add(new(s.ProductSlug, s.OfferId, s.Packs, s.ProductSlug, "", "Ambalaj indisponibil", 0, 1, 1, 0, null, null, null, null, "Produsul sau ambalajul nu mai este publicat."));
                continue;
            }

            string? issue = null;
            if (!CartQuantity.IsValid(s.Packs, o.UnitsPerPack, o.MinPacks, o.PackStep))
                issue = $"Alege minimum {o.MinPacks} ambalaje, cu pas {o.PackStep}, în limita a 2.147.483.647 bucăți.";
            else if (o.Net is null || o.Gross is null || o.Gross <= 0 || string.IsNullOrEmpty(o.Currency))
                issue = "Preț indisponibil.";
            else if (o.Gross * s.Packs > 1000000000m)
                issue = "Valoarea unei linii depășește limita de 1.000.000.000.";
            else if (requested[o.VariantId] > o.Available)
                issue = "Stoc insuficient pentru toate ambalajele acestui SKU din coș.";
            lines.Add(new(s.ProductSlug, s.OfferId, s.Packs, o.ProductName, o.Sku, o.Label, o.UnitsPerPack, o.MinPacks, o.PackStep, (long)s.Packs * o.UnitsPerPack, o.Gross, issue is null ? decimal.Round(o.Net!.Value * s.Packs, 2, MidpointRounding.AwayFromZero) : null, issue is null ? o.Gross * s.Packs : null, o.Currency, issue, o.VariantId, o.Net));
        }

        var currencies = lines.Where(l => l.Currency is not null).Select(l => l.Currency).Distinct().ToArray();
        if (currencies.Length > 1)
            lines = lines.Select(l => l with { Issue = l.Issue ?? "Ambalajele trebuie să aibă aceeași monedă." }).ToList();
        var valid = lines.Count > 0 && lines.All(l => l.Issue is null);
        return new(lines, valid ? currencies[0] : null, valid ? lines.Sum(l => l.NetTotal) : null, valid ? lines.Sum(l => l.GrossTotal) : null, DateTimeOffset.UtcNow);
    }
}
