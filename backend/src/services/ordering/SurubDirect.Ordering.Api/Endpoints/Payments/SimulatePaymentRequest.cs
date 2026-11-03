namespace SurubDirect.Ordering.Api.Endpoints;
public sealed record SimulatePaymentRequest(Guid OperationId, long ExpectedVersion, string? Action, string? Reason);
