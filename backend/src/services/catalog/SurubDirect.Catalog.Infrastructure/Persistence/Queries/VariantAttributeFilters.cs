using SurubDirect.BuildingBlocks;
using SurubDirect.Catalog.Contracts;
using SurubDirect.Catalog.Domain;

namespace SurubDirect.Catalog.Infrastructure;
internal static class VariantAttributeFilters
{
    public static IQueryable<Variant> Apply(IQueryable<Variant> query, IReadOnlyList<AttributeFilter>? filters, IReadOnlyList<TechnicalAttribute> allowed)
    {
        if (filters is null) return query;
        if (filters.Count > 20 || filters.Select(x => x.AttributeId).Distinct().Count() != filters.Count)
            throw new BusinessException("Maximum 20 de filtre distincte.");
        foreach (var filter in filters)
        {
            var definition = allowed.SingleOrDefault(x => x.Id == filter.AttributeId) ?? throw new BusinessException("Filtru nepermis pentru această categorie.");
            if (definition.Kind == AttributeKind.Text)
            {
                if (filter.Minimum is not null || filter.Maximum is not null) throw new BusinessException("Filtrul text nu acceptă interval numeric.");
                var text = Guard.Text(filter.Text, "Valoare filtru", 120).ToUpperInvariant();
                query = definition.Source switch
                {
                    AttributeSource.Material => query.Where(v => v.Material.ToUpper() == text),
                    AttributeSource.Finish => query.Where(v => v.Finish.ToUpper() == text),
                    _ => query.Where(v => v.AttributeValues.Any(x => x.AttributeId == filter.AttributeId && x.NormalizedText == text))
                };
            }
            else
            {
                var min = filter.Minimum;
                var max = filter.Maximum;
                if (filter.Text is not null || (min is null && max is null) || min > max ||
                    (min.HasValue && (min.Value < -999999999999.999999m || min.Value > 999999999999.999999m)) || (max.HasValue && (max.Value < -999999999999.999999m || max.Value > 999999999999.999999m)))
                    throw new BusinessException("Interval numeric invalid.");
                query = definition.Source switch
                {
                    AttributeSource.Diameter => query.Where(v => v.DiameterMm != null && (min == null || v.DiameterMm >= min) && (max == null || v.DiameterMm <= max)),
                    AttributeSource.Length => query.Where(v => v.LengthMm != null && (min == null || v.LengthMm >= min) && (max == null || v.LengthMm <= max)),
                    _ => query.Where(v => v.AttributeValues.Any(x => x.AttributeId == filter.AttributeId && x.NumberValue != null && (min == null || x.NumberValue >= min) && (max == null || x.NumberValue <= max)))
                };
            }
        }
        return query;
    }
}
