using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Domain;
using Xunit;
namespace SurubDirect.Tests;
public sealed class DemoDeliveryRatesTests
{
    [Theory]
    [InlineData("unknown", "RON", "RO")]
    [InlineData("", "RON", "RO")]
    [InlineData("demo-standard", "EUR", "RO")]
    [InlineData("demo-standard", "RON", "DE")]
    public void Unsupported_delivery_is_not_silently_free(string code, string currency, string country) =>
        Assert.Throws<BusinessException>(() => DemoDeliveryRates.Select(code, currency, country));

    [Fact]
    public void No_selection_preserves_the_explicitly_excluded_transport() =>
        Assert.Null(DemoDeliveryRates.Select(null, "RON", "RO"));
}
