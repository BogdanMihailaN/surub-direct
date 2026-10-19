using Microsoft.Extensions.DependencyInjection;
using SurubDirect.Store.Application.Imports;
namespace SurubDirect.Store.Infrastructure;
public static class CatalogImportsModule
{
    public static IServiceCollection AddCatalogImports(this IServiceCollection services, string connection)
    {
        services.AddScoped<IProductImportRepository>(_ => new ProductImportRepository(connection));
        services.AddScoped<ImportBatchBuilder>();
        services.AddScoped<PreviewImportHandler>();
        services.AddScoped<CommitImportHandler>();
        return services;
    }
}
