using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SurubDirect.Ordering.Infrastructure;
internal sealed class FailedReservationMessageConfiguration : IEntityTypeConfiguration<FailedReservationMessage>
{
    public void Configure(EntityTypeBuilder<FailedReservationMessage> b)
    {
        b.ToTable("FailedReservationMessages", t => t.HasCheckConstraint("CK_FailedReservationMessages_RemovedPayload", "\"PayloadRemovedAt\" IS NULL OR (\"PublishedAt\" IS NOT NULL AND \"RequestedAt\" IS NOT NULL AND \"ReplayRequestId\" IS NOT NULL AND \"EventId\" IS NOT NULL AND \"PayloadHash\" IS NOT NULL AND octet_length(\"Payload\") = 0)"));
        b.HasKey(x => x.Id);
        b.Property(x => x.PayloadHash).HasMaxLength(64);
        b.HasIndex(x => new { x.PublishedAt, x.Id }).HasFilter("\"PublishedAt\" IS NOT NULL AND \"PayloadRemovedAt\" IS NULL");
        b.Property(x => x.Fingerprint).HasMaxLength(64);
        b.HasIndex(x => x.Fingerprint).IsUnique();
        b.Property(x => x.MessageId).HasMaxLength(200);
        b.Property(x => x.MessageType).HasMaxLength(200);
        b.Property(x => x.ReplayReason).HasMaxLength(500);
        b.Property(x => x.RequestedBy).HasMaxLength(200);
        b.HasIndex(x => x.ReplayRequestId).IsUnique();
        b.HasIndex(x => new { x.PublishedAt, x.RequestedAt });
    }
}
