using Microsoft.Extensions.DependencyInjection;
using SurubDirect.Catalog.Application;
using SurubDirect.Catalog.Contracts;
using SurubDirect.Pricing.Application;
using SurubDirect.Store.Application;

namespace SurubDirect.Catalog.Api.Bootstrap;
internal static class DemoData
{
    public static async Task Seed(IServiceProvider services)
    {
        var reader = services.GetRequiredService<ICatalogReader>();
        var createProduct = services.GetRequiredService<CreateProductHandler>();
        var addVariant = services.GetRequiredService<AddVariantHandler>();
        var addOffer = services.GetRequiredService<AddOfferHandler>();
        var prices = services.GetRequiredService<SetOfferPriceHandler>();
        var store = services.GetRequiredService<PublishStoreProductHandler>();
        if ((await reader.List(new(PageSize: 1), true, default)).Total > 0)
            return;
        (string Name, string Slug, string Category, string Sku, string Label, decimal? Diameter, decimal? Length, string Material, int Units, decimal Price, int Quantity)[] examples = [("Șurub hexagonal zincat", "surub-hexagonal-zincat", "Șuruburi metrice", "SH-M8-40-ZN", "M8 × 40 mm", 8, 40, "Oțel", 100, 45, 1200), ("Șurub cu locaș hexagonal", "surub-locas-hexagonal", "Șuruburi metrice", "SI-M6-30", "M6 × 30 mm", 6, 30, "Oțel", 100, 39, 500), ("Șurub pentru lemn", "surub-pentru-lemn", "Șuruburi pentru lemn", "SL-4-40", "4 × 40 mm", 4, 40, "Oțel", 200, 24, 2000), ("Șurub autoperforant", "surub-autoperforant", "Șuruburi autoperforante", "SAP-4.8-25", "4,8 × 25 mm", 4.8m, 25, "Oțel", 100, 28, 1000), ("Șurub pentru gips-carton", "surub-gips-carton", "Șuruburi gips-carton", "SGC-3.5-25", "3,5 × 25 mm", 3.5m, 25, "Oțel", 1000, 55, 5000), ("Piuliță hexagonală", "piulita-hexagonala", "Piulițe", "PH-M8-ZN", "M8", 8, null, "Oțel", 100, 12, 2500), ("Piuliță autoblocantă", "piulita-autoblocanta", "Piulițe", "PA-M8", "M8", 8, null, "Oțel", 100, 23, 800), ("Șaibă plată zincată", "saiba-plata-zincata", "Șaibe", "SP-M8-ZN", "Pentru M8", 8, null, "Oțel", 100, 6, 5000), ("Șaibă cu diametru mărit", "saiba-diametru-marit", "Șaibe", "SLG-M8-ZN", "Pentru M8", 8, null, "Oțel", 100, 11, 1000), ("Tijă filetată zincată", "tija-filetata-zincata", "Tije filetate", "TF-M8-1000", "M8 × 1000 mm", 8, 1000, "Oțel", 1, 7.50m, 120), ("Diblu universal din nylon", "diblu-nylon", "Dibluri", "DN-8-40", "8 × 40 mm", 8, 40, "Nylon", 100, 15, 2000), ("Ancoră metalică mecanică", "ancora-metalica", "Ancore", "AM-M10-100", "M10 × 100 mm", 10, 100, "Oțel", 25, 87.50m, 150), ("Nit orb din aluminiu", "nit-orb-aluminiu", "Nituri", "NIT-4-10-AL", "4 × 10 mm", 4, 10, "Aluminiu", 500, 60, 3000), ("Piuliță nituibilă", "piulita-nituibila", "Piulițe nituibile", "PN-M6", "M6", 6, null, "Oțel", 100, 48, 0)];
        foreach (var x in examples)
        {
            var id = await createProduct.Handle(new(x.Name, x.Slug, x.Category, "Produs demonstrativ pentru validarea catalogului Șurub Direct. Specificațiile, imaginile și prețurile sunt ilustrative; nu este disponibil pentru cumpărare."), default);
            var v = await addVariant.Handle(id, new(x.Sku, x.Label, x.Diameter, x.Length, x.Material, x.Material == "Oțel" ? "Zincat" : "Natural"), default);
            var o = await addOffer.Handle(id, v, new(x.Sku + "-P" + x.Units, x.Units == 1 ? "Bucată" : $"Cutie {x.Units} buc.", x.Units), default);
            // A configurable demonstration rate, not a tax determination for real merchandise.
            await prices.Handle(o, new(x.Price, 0.21m), default);
            if (x.Sku == "SH-M8-40-ZN")
            {
                var piece = await addOffer.Handle(id, v, new("SH-M8-40-BUC", "Bucată", 1, 10, 10), default);
                await prices.Handle(piece, new(0.55m, 0.21m), default);
                var extra = await addVariant.Handle(id, new("SH-M8-60-ZN", "M8 × 60 mm", 8, 60, "Oțel", "Zincat"), default);
                var extraOffer = await addOffer.Handle(id, extra, new("SH-M8-60-C100", "Cutie 100 buc.", 100), default);
                await prices.Handle(extraOffer, new(58, 0.21m), default);
            }

            await store.Handle(id, default);
        }
    }
}
