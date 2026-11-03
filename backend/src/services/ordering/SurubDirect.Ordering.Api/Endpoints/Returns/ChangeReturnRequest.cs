namespace SurubDirect.Ordering.Api.Endpoints;
public sealed record ChangeReturnRequest(Guid OperationId, long ExpectedVersion, string? Target, string? Reason, bool RestockConfirmed);
