namespace SurubDirect.Ordering.Api.Endpoints;
public sealed record DecidePartialReturnRequest(Guid OperationId, long ExpectedVersion, string? Target, string? Reason);
