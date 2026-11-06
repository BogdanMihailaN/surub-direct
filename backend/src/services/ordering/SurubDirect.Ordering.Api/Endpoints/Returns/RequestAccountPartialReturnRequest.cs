using SurubDirect.Ordering.Contracts;
namespace SurubDirect.Ordering.Api.Endpoints;
public sealed record RequestAccountPartialReturnRequest(string? ExpectedAccountId, Guid RequestId, IReadOnlyList<PartialReturnSelectionDto>? Lines, string? Reason);
