namespace SurubDirect.Messaging;
public sealed record BrokerTopology(string Exchange, string Queue, string RoutingKey, string DeadLetterQueue);
