namespace SurubDirect.Ordering.Contracts;
public sealed record OrderReturnDto(string Status, long Version, DateTimeOffset? RestoredAt, IReadOnlyList<ReturnChangeDto> History);
