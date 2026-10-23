namespace SurubDirect.Messaging;
public sealed record BrokerSettings(string Host, int Port, string VirtualHost, string Username, string Password);
