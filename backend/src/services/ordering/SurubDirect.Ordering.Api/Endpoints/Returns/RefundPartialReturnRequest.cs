namespace SurubDirect.Ordering.Api.Endpoints;
public sealed record RefundPartialReturnRequest(Guid OperationId, long ExpectedVersion, string? Reason);
