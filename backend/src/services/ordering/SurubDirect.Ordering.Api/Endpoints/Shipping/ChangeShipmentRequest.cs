namespace SurubDirect.Ordering.Api.Endpoints;
public sealed record ChangeShipmentRequest(Guid OperationId, long ExpectedVersion, string? Target, string? Reason);
