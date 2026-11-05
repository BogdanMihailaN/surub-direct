namespace SurubDirect.Ordering.Api.Endpoints;
public sealed record ReceivePartialReturnRequest(Guid OperationId, long ExpectedVersion, string? Reason, bool ResalableConfirmed);
