using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.Catalog.Application;
using SurubDirect.Catalog.Contracts;

namespace SurubDirect.Catalog.Infrastructure;
public static class CatalogModule
{
    public static IServiceCollection AddCatalog(this IServiceCollection s, string connection)
    {
        s.AddDbContext<CatalogDbContext>(o => o.UseNpgsql(connection, n => n.MigrationsHistoryTable("__EFMigrationsHistory", "catalog")));
        s.AddScoped<ITaxonomyRepository, TaxonomyRepository>();
        s.AddScoped<ITaxonomyReader, TaxonomyReader>();
        s.AddScoped<CatalogRepository>();
        s.AddScoped<ICatalogRepository>(p => p.GetRequiredService<CatalogRepository>());
        s.AddScoped<ICatalogReader, CatalogReader>();
        s.AddScoped<UpdateProductHandler>();
        s.AddScoped<ArchiveProductHandler>();
        s.AddScoped<RestoreProductHandler>();
        s.AddScoped<UpdateVariantHandler>();
        s.AddScoped<UpdateOfferHandler>();
        s.AddScoped<CreateCategoryHandler>();
        s.AddScoped<UpdateCategoryHandler>();
        s.AddScoped<CreateAttributeHandler>();
        s.AddScoped<UpdateAttributeHandler>();
        s.AddScoped<SetVariantAttributesHandler>();
        s.AddScoped<CreateProductHandler>();
        s.AddScoped<AddVariantHandler>();
        s.AddScoped<AddOfferHandler>();
        s.AddScoped<PublishProductHandler>();
        s.AddScoped<ICatalogOutbox, CatalogOutbox>();
        return s;
    }

    public static Task MigrateCatalog(this IServiceProvider s, CancellationToken ct = default) => s.GetRequiredService<CatalogDbContext>().Database.MigrateAsync(ct);
}
