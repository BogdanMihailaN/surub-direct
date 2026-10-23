using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SurubDirect.Ordering.Infrastructure;
internal sealed class FailedReservationMessageConfiguration : IEntityTypeConfiguration<FailedReservationMessage>
{
    public void Configure(EntityTypeBuilder<FailedReservationMessage> b)
    {
        b.ToTable("FailedReservationMessages");
        b.HasKey(x => x.Id);
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
