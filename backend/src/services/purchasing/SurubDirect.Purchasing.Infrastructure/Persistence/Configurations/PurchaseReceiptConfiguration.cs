using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurubDirect.Purchasing.Domain;
namespace SurubDirect.Purchasing.Infrastructure;
internal sealed class PurchaseReceiptConfiguration : IEntityTypeConfiguration<PurchaseReceipt>
{
    public void Configure(EntityTypeBuilder<PurchaseReceipt> b)
    {
        b.Property(r => r.Id).ValueGeneratedNever();
        b.Property(r => r.Reference).HasMaxLength(100); b.Property(r => r.Actor).HasMaxLength(200);
        b.HasIndex(r => new { r.LineId, r.Reference }).IsUnique();
        b.HasOne<PurchaseOrderLine>().WithMany().HasForeignKey(r => r.LineId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PurchaseOrder>().WithMany(o => o.Receipts).HasForeignKey(r => r.OrderId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable("PurchaseReceipts", t => t.HasCheckConstraint("CK_PurchaseReceipt_Quantity", "\"Quantity\" > 0 AND \"Quantity\" <= 10000000"));
    }
}
