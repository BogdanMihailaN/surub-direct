using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.Ordering.Api.Configuration;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Domain;
using SurubDirect.Ordering.Infrastructure.Delivery;
using SurubDirect.BuildingBlocks;
using Xunit;

namespace SurubDirect.Tests;
public sealed class ConfiguredDeliveryRatesTests
{
    private static DeliveryRateSettings Rate() => new() { Code = "standard", Label = "Livrare demo", NetAmount = 10, GrossAmount = 12.10m };

    [Theory]
    [InlineData(0, 12.10)]
    [InlineData(-1, 12.10)]
    [InlineData(13, 12.10)]
    [InlineData(10.001, 12.10)]
    [InlineData(10, 12.101)]
    [InlineData(10000, 10000.01)]
    public void Invalid_amounts_fail_configuration(decimal net, decimal gross)
    {
        var rate = Rate(); rate.NetAmount = net; rate.GrossAmount = gross;
        Assert.Throws<InvalidOperationException>(() => new ConfiguredDeliveryRates([rate]));
    }

    [Theory]
    [InlineData("Standard", "Livrare", "RON")]
    [InlineData("standard space", "Livrare", "RON")]
    [InlineData("", "Livrare", "RON")]
    [InlineData("standard", " ", "RON")]
    [InlineData("standard", "Livrare\n", "RON")]
    [InlineData("standard", "Livrare", "EUR")]
    public void Invalid_identifiers_labels_and_currency_are_rejected(string code, string label, string currency)
    {
        var rate = Rate(); rate.Code = code; rate.Label = label; rate.Currency = currency;
        Assert.Throws<InvalidOperationException>(() => new ConfiguredDeliveryRates([rate]));
    }

    [Fact]
    public void Duplicate_or_excessive_methods_are_rejected_including_disabled_duplicates()
    {
        var disabled = Rate(); disabled.Enabled = false;
        Assert.Throws<InvalidOperationException>(() => new ConfiguredDeliveryRates([Rate(), disabled]));
        var many = Enumerable.Range(0, 21).Select(i => new DeliveryRateSettings { Code = "rate-" + i, Label = "Demo", NetAmount = 1, GrossAmount = 1 }).ToArray();
        Assert.Throws<InvalidOperationException>(() => new ConfiguredDeliveryRates(many));
    }

    [Fact]
    public void Disabled_and_missing_methods_never_fall_back_to_an_implicit_price()
    {
        var disabled = Rate(); disabled.Enabled = false;
        var rates = new ConfiguredDeliveryRates([disabled]);
        Assert.Empty(rates.Options);
        Assert.Empty(new ConfiguredDeliveryRates([]).Options);
        Assert.Throws<BusinessException>(() => DeliverySelection.Select(rates.Options, "standard", "RON", "RO"));
    }

    [Fact]
    public void Configuration_is_copied_and_published_values_are_read_only()
    {
        var setting = Rate();
        var rates = new ConfiguredDeliveryRates([setting]);
        setting.GrossAmount = 99;
        setting.Label = "Changed";
        Assert.Equal(12.10m, Assert.Single(rates.Options).GrossAmount);
        Assert.Equal("Livrare demo", Assert.Single(rates.Options).Label);
        Assert.Throws<NotSupportedException>(() => ((IList<DeliveryCharge>)rates.Options).Clear());
    }

    [Theory]
    [InlineData("Development", 1)]
    [InlineData("Production", 0)]
    public void Registration_reads_configuration_only_in_development(string environment, int count)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Delivery:Methods:0:Code"] = "configured",
            ["Delivery:Methods:0:Label"] = "Configurat",
            ["Delivery:Methods:0:NetAmount"] = "25.50",
            ["Delivery:Methods:0:GrossAmount"] = "30.85"
        });
        builder.AddDeliveryRates();
        using var services = builder.Services.BuildServiceProvider();
        var options = services.GetRequiredService<IDeliveryRates>().Options;
        Assert.Equal(count, options.Count);
        if (count > 0) Assert.Equal(30.85m, options[0].GrossAmount);
    }
}
