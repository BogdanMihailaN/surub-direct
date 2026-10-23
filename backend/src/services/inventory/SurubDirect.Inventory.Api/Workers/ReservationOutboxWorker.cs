using SurubDirect.Messaging;
using SurubDirect.Inventory.Infrastructure;

namespace SurubDirect.Inventory.Api.Workers;
public sealed class ReservationOutboxWorker(IServiceScopeFactory scopes, BrokerSettings settings, BrokerTopology topology, ILogger<ReservationOutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var session = await RabbitSession.Open(settings, topology, ct);
                while (!ct.IsCancellationRequested)
                {
                    await using var scope = scopes.CreateAsyncScope();
                    var count = await scope.ServiceProvider.GetRequiredService<InventoryOutbox>().Process((id, body, token) => session.Publish(id.ToString(), "inventory.reservation-outcome.v1", body, token), ct);
                    if (count == 0)
                        await Task.Delay(1000, ct);
                }
            }
            catch (OperationCanceledException)when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception error)
            {
                logger.LogWarning("Reservation messaging interrupted ({ErrorType}); reconnecting in five seconds.", error.GetType().Name);
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
        }
    }
}
