using Microsoft.Extensions.DependencyInjection;
using SurubDirect.Catalog.Application;
using SurubDirect.Catalog.Infrastructure;
using Xunit;
namespace SurubDirect.Tests;
public class MediaRegistrationTests
{
    [Fact]
    public void Catalog_worker_registration_does_not_require_media_storage()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCatalog("Host=localhost;Database=test;Username=test;Password=test");
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICatalogOutbox>());
        Assert.Null(provider.GetService<IImageStorage>());
    }
}
