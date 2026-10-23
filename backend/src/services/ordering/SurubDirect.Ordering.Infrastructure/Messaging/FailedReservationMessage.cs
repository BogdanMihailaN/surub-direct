namespace SurubDirect.Ordering.Infrastructure;
public sealed class FailedReservationMessage
{
    public Guid Id { get; set; }
    public string Fingerprint { get; set; } = "";
    public string? MessageId { get; set; }
    public string? MessageType { get; set; }
    public Guid? PreviousReplayId { get; set; }
    public byte[] Payload { get; set; } = [];
    public int OriginalSize { get; set; }
    public int Attempts { get; set; }
    public bool CanReplay { get; set; }
    public DateTimeOffset CapturedAt { get; set; }
    public Guid? ReplayRequestId { get; set; }
    public string? ReplayReason { get; set; }
    public string? RequestedBy { get; set; }
    public DateTimeOffset? RequestedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
}
