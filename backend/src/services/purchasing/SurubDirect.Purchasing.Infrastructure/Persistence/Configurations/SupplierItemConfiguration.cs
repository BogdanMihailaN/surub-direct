using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurubDirect.Purchasing.Domain;
namespace SurubDirect.Purchasing.Infrastructure;
internal sealed class SupplierItemConfiguration : IEntityTypeConfiguration<SupplierItem>
{
    public void Configure(EntityTypeBuilder<SupplierItem> b)
    {
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => new { x.SupplierId, x.VariantId }).IsUnique();
        b.HasIndex(x => new { x.SupplierId, x.SupplierCode }).IsUnique();
        b.Property(x => x.NetCostPerPack).HasPrecision(18, 6);
        b.Property(x => x.Sku).HasMaxLength(80); b.Property(x => x.VariantLabel).HasMaxLength(160);
        b.Property(x => x.SupplierCode).HasMaxLength(80); b.Property(x => x.PackLabel).HasMaxLength(100); b.Property(x => x.Currency).HasMaxLength(3);
        b.ToTable("SupplierItems", t => t.HasCheckConstraint("CK_SupplierItem_Amounts", "\"UnitsPerPack\" > 0 AND \"NetCostPerPack\" > 0"));
    }
}
