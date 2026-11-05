using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurubDirect.Inventory.Domain;
namespace SurubDirect.Inventory.Infrastructure;
internal sealed class ReturnReceiptConfiguration : IEntityTypeConfiguration<ReturnReceipt>
{
    public void Configure(EntityTypeBuilder<ReturnReceipt> b)
    {
        b.ToTable("ReturnReceipts");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => x.ReservationId);
        b.HasOne<StockReservation>().WithMany().HasForeignKey(x => x.ReservationId).OnDelete(DeleteBehavior.Restrict);
        b.OwnsMany(x => x.Lines, lines =>
        {
            lines.ToTable("ReturnReceiptLines", t => t.HasCheckConstraint("CK_ReturnReceiptLines_Quantity", "\"Quantity\" > 0"));
            lines.WithOwner().HasForeignKey("ReceiptId");
            lines.HasKey("ReceiptId", nameof(ReturnReceiptLine.VariantId));
            lines.Property(x => x.VariantId).ValueGeneratedNever();
            lines.HasIndex(x => x.VariantId);
        });
    }
}
