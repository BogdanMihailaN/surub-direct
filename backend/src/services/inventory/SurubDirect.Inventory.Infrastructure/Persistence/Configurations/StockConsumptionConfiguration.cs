using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurubDirect.Inventory.Domain;

namespace SurubDirect.Inventory.Infrastructure;
internal sealed class StockConsumptionConfiguration : IEntityTypeConfiguration<StockConsumption>
{
    public void Configure(EntityTypeBuilder<StockConsumption> b)
    {
        b.ToTable("Consumptions");
        b.HasKey(x => new { x.ReservationId, x.VariantId });
        b.HasIndex(x => new { x.VariantId, x.CreatedAt });
        b.HasIndex(x => x.OrderId);
        b.HasOne<StockReservation>().WithMany().HasForeignKey(x => x.ReservationId).OnDelete(DeleteBehavior.Restrict);
    }
}
