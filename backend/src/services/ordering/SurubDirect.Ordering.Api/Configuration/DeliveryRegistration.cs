using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Infrastructure.Delivery;
namespace SurubDirect.Ordering.Api.Configuration;
public static class DeliveryRegistration
{
    public static void AddDeliveryRates(this WebApplicationBuilder builder)
    {
        var methods = builder.Environment.IsDevelopment()
            ? builder.Configuration.GetSection("Delivery:Methods").Get<DeliveryRateSettings[]>() ?? []
            : [];
        builder.Services.AddSingleton<IDeliveryRates>(new ConfiguredDeliveryRates(methods));
    }
}
