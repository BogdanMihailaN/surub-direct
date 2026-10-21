namespace SurubDirect.Ordering.Contracts;
public sealed record PreparedCartDto(Guid Id, string Status, bool CancellationRequested, DateTimeOffset CheckedAt, DateTimeOffset Deadline, DateTimeOffset? RemoteExpiresAt, string Currency, decimal NetTotal, decimal GrossTotal, CustomerDraftDto Customer, IReadOnlyList<PreparedCartLineDto> Lines);
