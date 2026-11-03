using System.Text.RegularExpressions;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Domain;
namespace SurubDirect.Ordering.Infrastructure.Delivery;
public sealed class ConfiguredDeliveryRates : IDeliveryRates
{
    public IReadOnlyList<DeliveryCharge> Options { get; }

    public ConfiguredDeliveryRates(IReadOnlyList<DeliveryRateSettings> settings)
    {
        if (settings.Count > 20)
            throw new InvalidOperationException("Delivery:Methods supports at most 20 methods.");
        var codes = new HashSet<string>(StringComparer.Ordinal);
        var active = new List<DeliveryCharge>();
        foreach (var rate in settings)
        {
            if (rate is null || string.IsNullOrEmpty(rate.Code) || rate.Code.Length > 64
                || !Regex.IsMatch(rate.Code, "^[a-z0-9]+(?:-[a-z0-9]+)*$") || !codes.Add(rate.Code)
                || string.IsNullOrWhiteSpace(rate.Label) || rate.Label.Length > 120 || rate.Label.Any(char.IsControl)
                || rate.Currency != "RON" || rate.NetAmount <= 0 || rate.GrossAmount < rate.NetAmount
                || rate.GrossAmount > 10000 || decimal.Round(rate.NetAmount, 2) != rate.NetAmount
                || decimal.Round(rate.GrossAmount, 2) != rate.GrossAmount)
                throw new InvalidOperationException("Invalid Delivery:Methods configuration. Use unique codes, labels, RON and positive two-decimal amounts (net <= gross <= 10000).");
            if (rate.Enabled)
                active.Add(new(rate.Code, rate.Label.Trim(), rate.Currency, rate.NetAmount, rate.GrossAmount));
        }
        // Copy immutable values; subsequent configuration mutations cannot change a running quotation.
        Options = active.AsReadOnly();
    }
}
