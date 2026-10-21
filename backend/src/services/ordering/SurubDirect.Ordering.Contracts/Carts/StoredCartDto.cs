namespace SurubDirect.Ordering.Contracts;
public sealed record StoredCartDto(Guid Id, int Version, DateTimeOffset UpdatedAt, DateTimeOffset ExpiresAt, IReadOnlyList<StoredCartLineDto> Lines, CustomerDraftDto? Customer);
