using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurubDirect.Ordering.Domain;
namespace SurubDirect.Ordering.Infrastructure;
internal sealed class PartialReturnRequestConfiguration : IEntityTypeConfiguration<PartialReturnRequest>
{
    public void Configure(EntityTypeBuilder<PartialReturnRequest> b)
    {
        b.ToTable("PartialReturns"); b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => new { x.OrderId, x.Status });
        b.HasIndex(x => new { x.Status, x.NextReceiptAttemptAt });
        b.Property(x => x.ReceiptReason).HasMaxLength(500);
        b.Property(x => x.ReceivedBy).HasMaxLength(200);
        b.Property(x => x.ReceiptError).HasMaxLength(200);
        b.Property(x => x.Status).HasMaxLength(20);
        b.Property(x => x.Reason).HasMaxLength(500); b.Property(x => x.Actor).HasMaxLength(200);
        b.Property(x => x.DecisionReason).HasMaxLength(500); b.Property(x => x.DecidedBy).HasMaxLength(200);
        b.HasOne<DemoOrder>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        b.OwnsMany(x => x.Lines, lines =>
        {
            lines.ToTable("PartialReturnLines", t => t.HasCheckConstraint("CK_PartialReturnLines_Quantity", "\"Packs\" > 0 AND \"UnitsPerPack\" > 0 AND \"Quantity\" > 0"));
            lines.WithOwner().HasForeignKey("ReturnId"); lines.HasKey("ReturnId", nameof(PartialReturnLine.OfferId));
            lines.Property(x => x.OfferId).ValueGeneratedNever();
        });
    }
}
