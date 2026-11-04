namespace SurubDirect.Ordering.Api.Endpoints;
public sealed record RequestAccountReturnRequest(Guid OperationId, string? ExpectedAccountId, string? Reason);
