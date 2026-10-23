using SurubDirect.Messaging;
using SurubDirect.Ordering.Infrastructure;

namespace SurubDirect.Ordering.Api.Workers;
public sealed class ReservationEventsWorker(IServiceScopeFactory scopes, BrokerSettings settings, BrokerTopology topology, ILogger<ReservationEventsWorker> logger) : BackgroundService
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
                    var message = await session.Read(ct);
                    if (message is null)
                    {
                        await Task.Delay(1000, ct);
                        continue;
                    }

                    var dead = await DeliveryProcessor.Process(session, message, async token =>
                    {
                        await using var scope = scopes.CreateAsyncScope();
                        await scope.ServiceProvider.GetRequiredService<ReservationInbox>().Handle(message.MessageId, message.Type, message.Body, token);
                    }, error => error is InvalidReservationEventException, ct);
                    if (dead)
                        logger.LogWarning("Reservation event {MessageId} moved to dead-letter queue.", message.MessageId);
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
