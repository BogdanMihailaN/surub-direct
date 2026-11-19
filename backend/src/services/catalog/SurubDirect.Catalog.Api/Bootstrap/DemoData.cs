using Microsoft.Extensions.DependencyInjection;
using SurubDirect.Catalog.Application;
using SurubDirect.Catalog.Application.Pricing;
using SurubDirect.Catalog.Contracts;
using SurubDirect.Catalog.Contracts.Pricing;

namespace SurubDirect.Catalog.Api.Bootstrap;

internal static class DemoData
{
    private const string Description = "Produs demonstrativ pentru validarea catalogului Șurub Direct. Specificațiile, imaginile și prețurile sunt ilustrative; nu este disponibil pentru cumpărare.";

    public static async Task Seed(IServiceProvider services)
    {
        var reader = services.GetRequiredService<ICatalogReader>();
        var repository = services.GetRequiredService<ICatalogRepository>();
        var createProduct = services.GetRequiredService<CreateProductHandler>();
        var addVariant = services.GetRequiredService<AddVariantHandler>();
        var addOffer = services.GetRequiredService<AddOfferHandler>();
        var prices = services.GetRequiredService<SetOfferPriceHandler>();
        var priceReader = services.GetRequiredService<IPricingReader>();
        var store = services.GetRequiredService<PublishStoreProductHandler>();
        (string Name, string Slug, string Category, string Sku, string Label, decimal? Diameter, decimal? Length, string Material, int Units, decimal Price)[] examples =
        [
            ("Șurub hexagonal zincat", "surub-hexagonal-zincat", "Șuruburi metrice", "SH-M8-40-ZN", "M8 × 40 mm", 8, 40, "Oțel", 100, 45),
            ("Șurub cu locaș hexagonal", "surub-locas-hexagonal", "Șuruburi metrice", "SI-M6-30", "M6 × 30 mm", 6, 30, "Oțel", 100, 39),
            ("Șurub pentru lemn", "surub-pentru-lemn", "Șuruburi pentru lemn", "SL-4-40", "4 × 40 mm", 4, 40, "Oțel", 200, 24),
            ("Șurub autoperforant", "surub-autoperforant", "Șuruburi autoperforante", "SAP-4.8-25", "4,8 × 25 mm", 4.8m, 25, "Oțel", 100, 28),
            ("Șurub pentru gips-carton", "surub-gips-carton", "Șuruburi gips-carton", "SGC-3.5-25", "3,5 × 25 mm", 3.5m, 25, "Oțel", 1000, 55),
            ("Piuliță hexagonală", "piulita-hexagonala", "Piulițe", "PH-M8-ZN", "M8", 8, null, "Oțel", 100, 12),
            ("Piuliță autoblocantă", "piulita-autoblocanta", "Piulițe", "PA-M8", "M8", 8, null, "Oțel", 100, 23),
            ("Șaibă plată zincată", "saiba-plata-zincata", "Șaibe", "SP-M8-ZN", "Pentru M8", 8, null, "Oțel", 100, 6),
            ("Șaibă cu diametru mărit", "saiba-diametru-marit", "Șaibe", "SLG-M8-ZN", "Pentru M8", 8, null, "Oțel", 100, 11),
            ("Tijă filetată zincată", "tija-filetata-zincata", "Tije filetate", "TF-M8-1000", "M8 × 1000 mm", 8, 1000, "Oțel", 1, 7.50m),
            ("Diblu universal din nylon", "diblu-nylon", "Dibluri", "DN-8-40", "8 × 40 mm", 8, 40, "Nylon", 100, 15),
            ("Ancoră metalică mecanică", "ancora-metalica", "Ancore", "AM-M10-100", "M10 × 100 mm", 10, 100, "Oțel", 25, 87.50m),
            ("Nit orb din aluminiu", "nit-orb-aluminiu", "Nituri", "NIT-4-10-AL", "4 × 10 mm", 4, 10, "Aluminiu", 500, 60),
            ("Piuliță nituibilă", "piulita-nituibila", "Piulițe nituibile", "PN-M6", "M6", 6, null, "Oțel", 100, 48)
        ];
        foreach (var example in examples)
        {
            var existingId = await repository.FindIdBySlug(example.Slug, default);
            var existing = existingId is Guid id ? await reader.Get(id, true, default) : null;
            if (existing is not null && (existing.Status != "Draft" || existing.Description != Description))
                continue;

            var product = existingId ?? await createProduct.Handle(new(example.Name, example.Slug, example.Category, Description), default);
            var current = existing?.Variants.SingleOrDefault(v => v.Sku == example.Sku);
            var finish = example.Material == "Oțel" ? "Zincat" : "Natural";
            var variant = current?.Id ?? await addVariant.Handle(product,
                new(example.Sku, example.Label, example.Diameter, example.Length, example.Material, finish), default);
            await EnsureOffer(variant, current, new(example.Sku + "-P" + example.Units,
                example.Units == 1 ? "Bucată" : $"Cutie {example.Units} buc.", example.Units), example.Price);

            if (example.Sku == "SH-M8-40-ZN")
            {
                await EnsureOffer(variant, current, new("SH-M8-40-BUC", "Bucată", 1, 10, 10), 0.55m);
                var extra = existing?.Variants.SingleOrDefault(v => v.Sku == "SH-M8-60-ZN");
                var extraId = extra?.Id ?? await addVariant.Handle(product,
                    new("SH-M8-60-ZN", "M8 × 60 mm", 8, 60, "Oțel", "Zincat"), default);
                await EnsureOffer(extraId, extra, new("SH-M8-60-C100", "Cutie 100 buc.", 100), 58);
            }

            await store.Handle(product, default);

            async Task EnsureOffer(Guid variantId, VariantDto? currentVariant, AddOffer input, decimal price)
            {
                var offerId = currentVariant?.Offers.SingleOrDefault(o => o.Code == input.Code)?.Id
                    ?? await addOffer.Handle(product, variantId, input, default);
                if ((await priceReader.GetPrices([offerId], default)).Count == 0)
                    await prices.Handle(offerId, new(price, 0.21m), default);
            }
        }
    }
}
