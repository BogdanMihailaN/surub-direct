namespace SurubDirect.Catalog.Infrastructure;
public sealed class PublicationActivity
{
    public Guid EventId { get; set; }
    public Guid ProductId { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
}
