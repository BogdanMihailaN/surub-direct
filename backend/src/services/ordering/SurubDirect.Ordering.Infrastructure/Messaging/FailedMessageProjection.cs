using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Infrastructure;
internal static class FailedMessageProjection
{
    public static FailedMessageDto Map(FailedReservationMessage x) => new(x.Id, x.MessageId, x.MessageType, x.PreviousReplayId, x.OriginalSize, x.Attempts, x.CanReplay, x.CapturedAt, x.ReplayRequestId, x.ReplayReason, x.RequestedBy, x.RequestedAt, x.PublishedAt, x.PayloadRemovedAt);
}
