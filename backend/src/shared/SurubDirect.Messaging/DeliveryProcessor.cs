namespace SurubDirect.Messaging;
public static class DeliveryProcessor
{
    public static async Task<bool> Process(RabbitSession session, BrokerDelivery message, Func<CancellationToken, Task> handle, Func<Exception, bool> permanent, CancellationToken ct)
    {
        var deadLettered = false;
        try
        {
            await handle(ct);
        }
        catch (Exception error)when (!ct.IsCancellationRequested)
        {
            var dead = permanent(error) || message.Attempt >= 4;
            deadLettered = dead;
            if (!dead)
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(30, 2 << message.Attempt)), ct);
            // Never ACK the original until its persistent replacement has a publisher confirmation.
            await session.Publish(message.MessageId ?? "invalid", message.Type ?? "unknown", message.Body, ct, message.Attempt + 1, dead);
        }

        await session.Ack(message.Tag, ct);
        return deadLettered;
    }
}
