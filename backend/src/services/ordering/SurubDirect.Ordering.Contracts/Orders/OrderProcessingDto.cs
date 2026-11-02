namespace SurubDirect.Ordering.Contracts;
public sealed record OrderProcessingDto(string Status, long Version, IReadOnlyList<OrderProcessingChangeDto> History);
