using SurubDirect.Messaging;
using SurubDirect.ServiceDefaults.Monitoring;
using SurubDirect.Inventory.Api.Workers;

namespace SurubDirect.Inventory.Api.Configuration;
public static class ReservationMessagingRegistration
{
    public static void AddReservationMessaging(this WebApplicationBuilder builder)
    {
        if (!builder.Configuration.GetValue<bool>("Messaging:Enabled"))
            return;
        string Required(string key) => builder.Configuration["Messaging:" + key] is { Length: > 0 } value ? value : throw new InvalidOperationException("Configure Messaging:" + key);
        builder.Services.AddOperationalMonitor<MessagingMonitorWorker>(builder.Configuration);
        builder.Services.AddSingleton(new BrokerSettings(Required("Host"), builder.Configuration.GetValue<int>("Messaging:Port", 5672), Required("VirtualHost"), Required("Username"), Required("Password")));
        builder.Services.AddSingleton(new BrokerTopology("inventory.reservations.v1", "ordering.reservation-outcomes.v1", "reservation.outcome", "ordering.reservation-outcomes.v1.dead"));
        builder.Services.AddHostedService<ReservationOutboxWorker>();
        builder.Services.AddOptions<OutboxRetentionOptions>().Bind(builder.Configuration.GetSection("Messaging:OutboxRetention")).ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddHostedService<OutboxRetentionWorker>();
    }
}
