using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SurubDirect.Inventory.Infrastructure;
internal sealed class InventoryEventConfiguration : IEntityTypeConfiguration<InventoryEvent>
{
    public void Configure(EntityTypeBuilder<InventoryEvent> b)
    {
        b.ToTable("ReservationOutbox", t => t.HasCheckConstraint("CK_ReservationOutbox_RemovedPayload", "\"PayloadRemovedAt\" IS NULL OR (\"PublishedAt\" IS NOT NULL AND \"PayloadHash\" IS NOT NULL AND \"Payload\" = '{}'::jsonb)"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).HasMaxLength(20);
        b.Property(x => x.Payload).HasColumnType("jsonb");
        b.Property(x => x.PayloadHash).HasMaxLength(64);
        b.HasIndex(x => new { x.PublishedAt, x.Id }).HasFilter("\"PublishedAt\" IS NOT NULL AND \"PayloadRemovedAt\" IS NULL");
        b.HasIndex(x => new { x.ReservationId, x.Status }).IsUnique();
        b.HasIndex(x => new { x.PublishedAt, x.OccurredAt });
    }
}
