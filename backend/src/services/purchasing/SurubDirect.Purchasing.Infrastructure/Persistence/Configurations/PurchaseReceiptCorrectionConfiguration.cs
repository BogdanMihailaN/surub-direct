using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurubDirect.Purchasing.Domain;
namespace SurubDirect.Purchasing.Infrastructure;

internal sealed class PurchaseReceiptCorrectionConfiguration : IEntityTypeConfiguration<PurchaseReceiptCorrection>
{
    public void Configure(EntityTypeBuilder<PurchaseReceiptCorrection> b)
    {
        b.Property(x => x.Id).ValueGeneratedNever(); b.Property(x => x.Reference).HasMaxLength(100); b.Property(x => x.Reason).HasMaxLength(500); b.Property(x => x.Actor).HasMaxLength(200);
        b.HasOne<PurchaseOrder>().WithMany(o => o.Corrections).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PurchaseReceipt>().WithMany().HasForeignKey(x => x.ReceiptId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ReceiptId, x.Reference }).IsUnique();
        b.ToTable("ReceiptCorrections", t => t.HasCheckConstraint("CK_Correction_Quantity", "\"Quantity\" BETWEEN 1 AND 10000000"));
    }
}
