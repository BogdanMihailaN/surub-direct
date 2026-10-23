using SurubDirect.Messaging;
using SurubDirect.ServiceDefaults.Monitoring;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Infrastructure;
using SurubDirect.Ordering.Api.Workers;

namespace SurubDirect.Ordering.Api.Configuration;
public static class ReservationMessagingRegistration
{
    public static void AddReservationMessaging(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<GetBrokerStatusHandler>();
        if (!builder.Configuration.GetValue<bool>("Messaging:Enabled"))
        {
            builder.Services.AddScoped<IBrokerMonitor, DisabledBrokerMonitor>();
            return;
        }

        builder.Services.AddScoped<IBrokerMonitor, RabbitBrokerMonitor>();
        string Required(string key) => builder.Configuration["Messaging:" + key] is { Length: > 0 } value ? value : throw new InvalidOperationException("Configure Messaging:" + key);
        builder.Services.AddOperationalMonitor<MessagingMonitorWorker>(builder.Configuration);
        builder.Services.AddSingleton(new BrokerSettings(Required("Host"), builder.Configuration.GetValue<int>("Messaging:Port", 5672), Required("VirtualHost"), Required("Username"), Required("Password")));
        builder.Services.AddSingleton(new BrokerTopology("inventory.reservations.v1", "ordering.reservation-outcomes.v1", "reservation.outcome", "ordering.reservation-outcomes.v1.dead"));
        builder.Services.AddHostedService<ReservationEventsWorker>();
        builder.Services.AddHostedService<FailedMessageWorker>();
    }
}
