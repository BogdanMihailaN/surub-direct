using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurubDirect.Inventory.Domain;

namespace SurubDirect.Inventory.Infrastructure;
internal sealed class StockRestorationConfiguration : IEntityTypeConfiguration<StockRestoration>
{
    public void Configure(EntityTypeBuilder<StockRestoration> b)
    {
        b.ToTable("Restorations");
        b.HasKey(x => new { x.ReservationId, x.VariantId });
        b.HasIndex(x => new { x.VariantId, x.CreatedAt });
        b.HasIndex(x => x.OrderId);
        b.HasOne<StockReservation>().WithMany().HasForeignKey(x => x.ReservationId).OnDelete(DeleteBehavior.Restrict);
    }
}
