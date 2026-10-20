using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurubDirect.Purchasing.Domain;
namespace SurubDirect.Purchasing.Infrastructure;
internal sealed class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> b)
    {
        b.Property(x => x.Id).ValueGeneratedNever(); b.HasIndex(x => new { x.OrderId, x.SupplierItemId }).IsUnique();
        b.Property(x => x.NetCostPerPack).HasPrecision(18, 6); b.Ignore(x => x.BaseUnits); b.Ignore(x => x.NetTotal);
        b.Property(x => x.Sku).HasMaxLength(80); b.Property(x => x.Label).HasMaxLength(160); b.Property(x => x.SupplierCode).HasMaxLength(80); b.Property(x => x.PackLabel).HasMaxLength(100);
        b.HasOne<SupplierItem>().WithMany().HasForeignKey(x => x.SupplierItemId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable("PurchaseOrderLines", t => t.HasCheckConstraint("CK_PurchaseOrderLine_Amounts", "\"Packs\" > 0 AND \"UnitsPerPack\" > 0 AND \"NetCostPerPack\" > 0 AND \"Packs\"::bigint * \"UnitsPerPack\" <= 2147483647"));
    }
}
