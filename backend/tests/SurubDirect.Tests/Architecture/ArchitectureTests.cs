using System.Xml.Linq;
using Xunit;

namespace SurubDirect.Tests;

public class ArchitectureTests
{
    private static string BackendRoot()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d != null && !File.Exists(Path.Combine(d.FullName, "SurubDirect.sln")))
            d = d.Parent;
        return d?.FullName ?? throw new InvalidOperationException("Backend solution root missing");
    }

    [Fact]
    public void Services_never_reference_another_service_assembly()
    {
        var root = BackendRoot();
        foreach (var owner in Directory.GetDirectories(Path.Combine(root, "src/services")))
            foreach (var file in Directory.GetFiles(owner, "*.csproj", SearchOption.AllDirectories))
                foreach (var reference in XDocument.Load(file).Descendants("ProjectReference"))
                {
                    var target = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, reference.Attribute("Include")!.Value));
                    Assert.True(target.StartsWith(owner + Path.DirectorySeparatorChar) || target.StartsWith(Path.Combine(root, "src/shared") + Path.DirectorySeparatorChar), target);
                }

        var gateway = XDocument.Load(Path.Combine(root, "src/gateway/SurubDirect.Gateway/SurubDirect.Gateway.csproj"));
        Assert.Empty(gateway.Descendants("ProjectReference"));
        Assert.DoesNotContain(gateway.Descendants("PackageReference"), p => p.Attribute("Include")!.Value.Contains("EntityFramework"));
    }

    [Fact]
    public void Store_application_has_no_framework_or_infrastructure_dependencies()
    {
        var xml = XDocument.Load(Path.Combine(BackendRoot(), "src/services/catalog/SurubDirect.Store.Application/SurubDirect.Store.Application.csproj"));
        Assert.Empty(xml.Descendants("PackageReference"));
        var allowed = new[] { "SurubDirect.Catalog.Application", "SurubDirect.Pricing.Domain", "SurubDirect.Pricing.Contracts" };
        Assert.All(xml.Descendants("ProjectReference"), r =>
            Assert.Contains(Path.GetFileNameWithoutExtension(r.Attribute("Include")!.Value), allowed));
    }

    [Theory]
    [InlineData("Catalog")]
    [InlineData("Pricing")]
    [InlineData("Inventory")]
    [InlineData("Purchasing")]
    [InlineData("Ordering")]
    public void Domain_is_independent_of_application_and_infrastructure(string module)
    {
        var file = Path.Combine(BackendRoot(), $"src/services/{(module is "Inventory" or "Purchasing" or "Ordering" ? module.ToLowerInvariant() : "catalog")}/SurubDirect.{module}.Domain/SurubDirect.{module}.Domain.csproj");
        var xml = XDocument.Load(file);
        Assert.Empty(xml.Descendants("PackageReference"));
        Assert.All(xml.Descendants("ProjectReference"), r => Assert.EndsWith("SurubDirect.BuildingBlocks.csproj", r.Attribute("Include")!.Value));
    }

    [Theory]
    [InlineData("Catalog")]
    [InlineData("Pricing")]
    [InlineData("Inventory")]
    [InlineData("Purchasing")]
    [InlineData("Ordering")]
    [InlineData("Identity")]
    public void Application_depends_only_on_own_domain_and_module_contracts(string module)
    {
        var file = Path.Combine(BackendRoot(), $"src/services/{(module is "Inventory" or "Purchasing" or "Ordering" or "Identity" ? module.ToLowerInvariant() : "catalog")}/SurubDirect.{module}.Application/SurubDirect.{module}.Application.csproj");
        var xml = XDocument.Load(file);
        Assert.Empty(xml.Descendants("PackageReference"));
        Assert.All(xml.Descendants("ProjectReference"), r =>
        {
            var name = Path.GetFileNameWithoutExtension(r.Attribute("Include")!.Value);
            Assert.True(name == $"SurubDirect.{module}.Domain" || name.EndsWith(".Contracts"), name);
        });
    }
}
