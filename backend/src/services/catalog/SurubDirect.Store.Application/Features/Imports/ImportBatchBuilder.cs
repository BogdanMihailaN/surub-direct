using SurubDirect.BuildingBlocks;
using SurubDirect.Catalog.Domain;
using SurubDirect.Pricing.Domain;
namespace SurubDirect.Store.Application.Imports;
public sealed class ImportBatchBuilder(IProductImportRepository repository)
{
    public async Task<ImportBatch> Build(string? csv, CancellationToken ct)
    {
        var records = CsvParser.Parse(csv);
        if (records.Count == 0 || !records[0].Fields.SequenceEqual(ImportCsvFormat.Header.Split(';')))
            throw new BusinessException("Antet CSV invalid. Folosește coloanele și ordinea din fișierul-model.");
        var errors = new List<ImportError>();
        var rows = new List<ImportRow>();
        foreach (var record in records.Skip(1))
        {
            try { rows.Add(ImportRowReader.Read(record)); }
            catch (BusinessException e) { errors.Add(new(record.Line, e.Message)); }
        }
        if (records.Count == 1) errors.Add(new(1, "Fișierul nu conține rânduri de date."));
        var lookup = await repository.Lookup(rows, ct);
        var products = new Dictionary<string, Product>();
        var variants = new Dictionary<string, Variant>();
        var productRows = new Dictionary<string, ImportRow>();
        var variantRows = new Dictionary<string, ImportRow>();
        var codes = new HashSet<string>();
        var prices = new List<OfferPrice>();
        foreach (var row in rows)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                if (lookup.Slugs.Contains(row.Slug)) throw new BusinessException("Slug-ul produsului există deja. Importul creează doar produse noi.");
                if (lookup.Skus.Contains(row.Sku)) throw new BusinessException("SKU-ul există deja în catalog.");
                if (lookup.OfferCodes.Contains(row.OfferCode)) throw new BusinessException("Codul ambalajului există deja în catalog.");
                if (!codes.Add(row.OfferCode)) throw new BusinessException("Cod de ambalaj duplicat în fișier.");
                var category = lookup.Categories.SingleOrDefault(x => x.Slug == row.CategorySlug)
                    ?? throw new BusinessException("Categoria nu există. Copiază slug-ul din lista categoriilor.");
                // Validate every row through the domain, including repeated product/SKU metadata.
                var candidate = Product.Create(row.ProductName, row.Slug, category.Name, row.Description);
                candidate.AssignCategory(category);
                var candidateVariant = candidate.AddVariant(row.Sku, row.VariantLabel, row.DiameterMm, row.LengthMm, row.Material, row.Finish);
                candidateVariant.AddOffer(row.OfferCode, row.OfferLabel, row.UnitsPerPack, row.MinPacks, row.PackStep);
                OfferPrice.Create(Guid.NewGuid(), row.NetAmount, row.TaxRate);
                if (productRows.TryGetValue(row.Slug, out var firstProduct) &&
                    (firstProduct.ProductName != row.ProductName || firstProduct.CategorySlug != row.CategorySlug || firstProduct.Description != row.Description))
                    throw new BusinessException($"Datele produsului diferă de linia {firstProduct.Line} pentru același slug.");
                if (variantRows.TryGetValue(row.Sku, out var firstVariant) &&
                    (firstVariant.Slug != row.Slug || firstVariant.VariantLabel != row.VariantLabel || firstVariant.DiameterMm != row.DiameterMm ||
                     firstVariant.LengthMm != row.LengthMm || firstVariant.Material != row.Material || firstVariant.Finish != row.Finish))
                    throw new BusinessException($"SKU duplicat cu date diferite față de linia {firstVariant.Line}.");
                if (!products.TryGetValue(row.Slug, out var product))
                {
                    product = Product.Create(row.ProductName, row.Slug, category.Name, row.Description);
                    product.AssignCategory(category);
                    products.Add(row.Slug, product); productRows.Add(row.Slug, row);
                }
                if (!variants.TryGetValue(row.Sku, out var variant))
                {
                    variant = product.AddVariant(row.Sku, row.VariantLabel, row.DiameterMm, row.LengthMm, row.Material, row.Finish);
                    variants.Add(row.Sku, variant); variantRows.Add(row.Sku, row);
                }
                var offer = variant.AddOffer(row.OfferCode, row.OfferLabel, row.UnitsPerPack, row.MinPacks, row.PackStep);
                prices.Add(OfferPrice.Create(offer.Id, row.NetAmount, row.TaxRate));
            }
            catch (BusinessException e) { errors.Add(new(row.Line, e.Message)); }
        }
        var preview = new ImportPreview(rows, errors.OrderBy(x => x.Line).ToArray(), products.Count, variants.Count, prices.Count);
        return new(preview, products.Values.ToArray(), prices);
    }
}
