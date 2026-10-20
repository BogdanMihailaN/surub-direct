using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurubDirect.Inventory.Domain;
namespace SurubDirect.Inventory.Infrastructure;

internal sealed class ReceiptCorrectionConfiguration : IEntityTypeConfiguration<ReceiptCorrection>
{
    public void Configure(EntityTypeBuilder<ReceiptCorrection> b)
    {
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Reference).HasMaxLength(100); b.Property(x => x.Reason).HasMaxLength(500); b.Property(x => x.Actor).HasMaxLength(200);
        b.HasOne<StockReceipt>().WithMany().HasForeignKey(x => x.ReceiptId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ReceiptId, x.Reference }).IsUnique(); b.HasIndex(x => new { x.VariantId, x.CreatedAt });
        b.ToTable("ReceiptCorrections", t => t.HasCheckConstraint("CK_ReceiptCorrection_Quantity", "\"Quantity\" BETWEEN 1 AND 10000000"));
    }
}
