using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurubDirect.Inventory.Domain;
namespace SurubDirect.Inventory.Infrastructure;
internal sealed class StockAdjustmentConfiguration : IEntityTypeConfiguration<StockAdjustment>
{
    public void Configure(EntityTypeBuilder<StockAdjustment> b)
    {
        b.Property(x => x.Id).ValueGeneratedNever(); b.HasIndex(x => new { x.VariantId, x.CreatedAt });
        b.Property(x => x.Reference).HasMaxLength(100); b.Property(x => x.Reason).HasMaxLength(500); b.Property(x => x.Actor).HasMaxLength(200);
        b.ToTable("Adjustments", t => t.HasCheckConstraint("CK_Adjustment_Quantity", "\"Quantity\" <> 0 AND \"Quantity\" BETWEEN -10000000 AND 10000000 AND \"ExpectedReserved\" >= 0 AND \"ExpectedOnHand\" >= \"ExpectedReserved\""));
    }
}
