namespace SurubDirect.Ordering.Contracts;
public sealed record PartialReturnDto(Guid Id, Guid OrderId, string Status, long Version, string Reason, string Actor, DateTimeOffset CreatedAt,
    IReadOnlyList<PartialReturnLineDto> Lines, Guid? DecisionId, string? DecisionReason, string? DecidedBy, DateTimeOffset? DecidedAt);
