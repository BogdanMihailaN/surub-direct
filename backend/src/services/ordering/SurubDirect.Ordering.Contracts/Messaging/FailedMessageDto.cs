namespace SurubDirect.Ordering.Contracts;
public sealed record FailedMessageDto(Guid Id, string? MessageId, string? MessageType, Guid? PreviousReplayId, int OriginalSize, int Attempts, bool CanReplay, DateTimeOffset CapturedAt, Guid? ReplayRequestId, string? ReplayReason, string? RequestedBy, DateTimeOffset? RequestedAt, DateTimeOffset? PublishedAt);
