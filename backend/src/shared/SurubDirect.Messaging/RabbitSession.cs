using RabbitMQ.Client;

namespace SurubDirect.Messaging;
public sealed class RabbitSession : IAsyncDisposable
{
    private readonly IConnection connection;
    private readonly IChannel channel;
    private readonly BrokerTopology topology;
    private RabbitSession(IConnection connection, IChannel channel, BrokerTopology topology)
    {
        this.connection = connection;
        this.channel = channel;
        this.topology = topology;
    }

    public static async Task<RabbitSession> Open(BrokerSettings settings, BrokerTopology topology, CancellationToken ct, bool declareTopology = true)
    {
        var factory = new ConnectionFactory
        {
            HostName = settings.Host,
            Port = settings.Port,
            VirtualHost = settings.VirtualHost,
            UserName = settings.Username,
            Password = settings.Password,
            AutomaticRecoveryEnabled = false,
            RequestedConnectionTimeout = TimeSpan.FromSeconds(5),
            RequestedHeartbeat = TimeSpan.FromSeconds(15)
        };
        var connection = await factory.CreateConnectionAsync(ct);
        try
        {
            var channel = await connection.CreateChannelAsync(new CreateChannelOptions(true, true), ct);
            var session = new RabbitSession(connection, channel, topology);
            if (!declareTopology)
                return session;
            await channel.ExchangeDeclareAsync(topology.Exchange, ExchangeType.Direct, true, false, cancellationToken: ct);
            var arguments = new Dictionary<string, object?>
            {
                ["x-queue-type"] = "quorum",
                ["x-delivery-limit"] = -1
            };
            await channel.QueueDeclareAsync(topology.Queue, true, false, false, arguments, cancellationToken: ct);
            await channel.QueueDeclareAsync(topology.DeadLetterQueue, true, false, false, arguments, cancellationToken: ct);
            await channel.QueueBindAsync(topology.Queue, topology.Exchange, topology.RoutingKey, cancellationToken: ct);
            await channel.QueueBindAsync(topology.DeadLetterQueue, topology.Exchange, topology.RoutingKey + ".dead", cancellationToken: ct);
            return session;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    public async Task Publish(string id, string type, byte[] body, CancellationToken ct, int attempt = 0, bool dead = false, Guid? replayId = null)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        var properties = new BasicProperties
        {
            MessageId = id,
            Type = type,
            ContentType = "application/json",
            Persistent = true,
            Headers = new Dictionary<string, object?>
            {
                ["attempt"] = attempt
            }
        };
        if (replayId is not null)
            properties.Headers!["replay-id"] = replayId.Value.ToString();
        await channel.BasicPublishAsync(topology.Exchange, topology.RoutingKey + (dead ? ".dead" : ""), true, properties, body, timeout.Token);
    }

    public async Task<BrokerDelivery?> Read(CancellationToken ct, bool dead = false)
    {
        var value = await channel.BasicGetAsync(dead ? topology.DeadLetterQueue : topology.Queue, false, ct);
        if (value is null)
            return null;
        var attempt = 0;
        if (value.BasicProperties.Headers?.TryGetValue("attempt", out var raw) == true)
            attempt = raw switch
            {
                int number => Math.Clamp(number, 0, 5),
                long number => (int)Math.Clamp(number, 0, 5),
                short number => Math.Clamp((int)number, 0, 5),
                byte number => Math.Clamp((int)number, 0, 5),
                _ => 0
            };
        Guid? replayId = null;
        if (value.BasicProperties.Headers?.TryGetValue("replay-id", out var replay) == true)
        {
            var text = replay is byte[] bytes ? System.Text.Encoding.UTF8.GetString(bytes) : replay?.ToString();
            if (Guid.TryParse(text, out var parsed))
                replayId = parsed;
        }

        return new(value.DeliveryTag, value.BasicProperties.MessageId, value.BasicProperties.Type, value.Body.ToArray(), attempt, replayId);
    }

    public Task<uint> ReadyCount(bool dead, CancellationToken ct) => channel.MessageCountAsync(dead ? topology.DeadLetterQueue : topology.Queue, ct);
    public ValueTask Ack(ulong tag, CancellationToken ct) => channel.BasicAckAsync(tag, false, ct);
    public async ValueTask DisposeAsync()
    {
        await channel.DisposeAsync();
        await connection.DisposeAsync();
    }
}
