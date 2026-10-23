namespace SurubDirect.Messaging;
public sealed record BrokerDelivery(ulong Tag, string? MessageId, string? Type, byte[] Body, int Attempt);
