namespace SurubDirect.Ordering.Api.Endpoints;
public sealed record RetryPartialReturnReceiptRequest(Guid OperationId, long ExpectedVersion, string? Reason);
