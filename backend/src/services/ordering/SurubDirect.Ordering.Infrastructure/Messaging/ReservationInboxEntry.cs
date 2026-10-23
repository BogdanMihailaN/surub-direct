namespace SurubDirect.Ordering.Infrastructure;
public sealed class ReservationInboxEntry
{
    public Guid Id { get; set; }
    public string PayloadHash { get; set; } = "";
    public DateTimeOffset ReceivedAt { get; set; }
}
