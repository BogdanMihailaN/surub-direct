using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurubDirect.Purchasing.Domain;
namespace SurubDirect.Purchasing.Infrastructure;
internal sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> b)
    {
        b.Property(x => x.Id).ValueGeneratedNever(); b.Property(x => x.Number).UseIdentityAlwaysColumn(); b.HasIndex(x => x.Number).IsUnique();
        b.Property(x => x.Version).IsConcurrencyToken(); b.Ignore(x => x.NetTotal);
        b.Property(x => x.SupplierName).HasMaxLength(200); b.Property(x => x.SupplierCountry).HasMaxLength(2); b.Property(x => x.SupplierTaxId).HasMaxLength(40);
        b.Property(x => x.SupplierEmail).HasMaxLength(254); b.Property(x => x.SupplierAddress).HasMaxLength(500);
        b.Property(x => x.Currency).HasMaxLength(3); b.Property(x => x.Notes).HasMaxLength(2000); b.Property(x => x.CancellationReason).HasMaxLength(500);
        b.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.CreatedAt, x.Id });
    }
}
