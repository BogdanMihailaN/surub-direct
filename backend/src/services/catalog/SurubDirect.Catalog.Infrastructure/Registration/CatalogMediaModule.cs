using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SurubDirect.Catalog.Application;
namespace SurubDirect.Catalog.Infrastructure;
public static class CatalogMediaModule
{
    public static IServiceCollection AddCatalogMedia(this IServiceCollection services, string root)
    {
        services.AddScoped<UploadImageHandler>();
        services.AddScoped<UpdateImageHandler>();
        services.AddScoped<ArrangeImagesHandler>();
        services.AddScoped<DeleteImageHandler>();
        services.AddScoped<GetImageHandler>();
        services.AddSingleton<IImageProcessor, SkiaImageProcessor>();
        services.AddSingleton<IImageStorage>(p => new LocalImageStorage(root, p.GetRequiredService<ILogger<LocalImageStorage>>()));
        return services;
    }
}
