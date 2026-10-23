using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SurubDirect.Inventory.Infrastructure;
internal sealed class InventoryEventConfiguration : IEntityTypeConfiguration<InventoryEvent>
{
    public void Configure(EntityTypeBuilder<InventoryEvent> b)
    {
        b.ToTable("ReservationOutbox");
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).HasMaxLength(20);
        b.Property(x => x.Payload).HasColumnType("jsonb");
        b.HasIndex(x => new { x.ReservationId, x.Status }).IsUnique();
        b.HasIndex(x => new { x.PublishedAt, x.OccurredAt });
    }
}
