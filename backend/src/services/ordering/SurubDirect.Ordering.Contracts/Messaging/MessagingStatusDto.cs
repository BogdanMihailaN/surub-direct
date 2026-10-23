namespace SurubDirect.Ordering.Contracts;
public sealed record MessagingStatusDto(int InboxTotal, DateTimeOffset? LastReceivedAt, int AwaitingReview, int PendingReplays, IReadOnlyList<FailedMessageDto> Items, int Total, int Page, int PageSize);
