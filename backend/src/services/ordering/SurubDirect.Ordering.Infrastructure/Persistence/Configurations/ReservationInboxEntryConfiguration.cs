using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SurubDirect.Ordering.Infrastructure;
internal sealed class ReservationInboxEntryConfiguration : IEntityTypeConfiguration<ReservationInboxEntry>
{
    public void Configure(EntityTypeBuilder<ReservationInboxEntry> b)
    {
        b.ToTable("ReservationInbox");
        b.HasKey(x => x.Id);
        b.Property(x => x.PayloadHash).HasMaxLength(64);
    }
}
