using SurubDirect.Ordering.Contracts;
namespace SurubDirect.Ordering.Api.Endpoints;
public sealed record CreatePartialReturnRequest(Guid RequestId, IReadOnlyList<PartialReturnSelectionDto>? Lines, string? Reason);
