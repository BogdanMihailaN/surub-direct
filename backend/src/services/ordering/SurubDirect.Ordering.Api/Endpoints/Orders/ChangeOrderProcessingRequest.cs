namespace SurubDirect.Ordering.Api.Endpoints;
public sealed record ChangeOrderProcessingRequest(Guid OperationId, long ExpectedVersion, string? Target, string? Reason);
