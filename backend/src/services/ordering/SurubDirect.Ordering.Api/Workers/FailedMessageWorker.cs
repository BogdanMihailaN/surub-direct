using SurubDirect.Messaging;
using SurubDirect.Ordering.Infrastructure;

namespace SurubDirect.Ordering.Api.Workers;
public sealed class FailedMessageWorker(IServiceScopeFactory scopes, BrokerSettings settings, BrokerTopology topology, ILogger<FailedMessageWorker> logger) : BackgroundService
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
                    var store = scope.ServiceProvider.GetRequiredService<FailedMessageStore>();
                    var message = await session.Read(ct, true);
                    if (message is not null)
                    {
                        await store.Capture(message, ct);
                        await session.Ack(message.Tag, ct);
                    }

                    var sent = await store.PublishPending((delivery, token) => session.Publish(delivery.MessageId!, delivery.Type!, delivery.Body, token, replayId: delivery.ReplayId), ct);
                    if (message is null && sent == 0)
                        await Task.Delay(1000, ct);
                }
            }
            catch (OperationCanceledException)when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception error)
            {
                logger.LogWarning("Failed-message processing interrupted ({ErrorType}); retry in five seconds.", error.GetType().Name);
                await Task.Delay(5000, ct);
            }
        }
    }
}
