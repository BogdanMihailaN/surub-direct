namespace SurubDirect.Ordering.Contracts;
public sealed record AccountPartialReturnDto(Guid Id, string Status, DateTimeOffset CreatedAt, DateTimeOffset? InventoryReceivedAt, IReadOnlyList<AccountPartialReturnLineDto> Lines);
