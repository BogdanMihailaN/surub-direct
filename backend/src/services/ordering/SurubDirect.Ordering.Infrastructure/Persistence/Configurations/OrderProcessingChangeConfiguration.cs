using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurubDirect.Ordering.Domain;
namespace SurubDirect.Ordering.Infrastructure;
internal sealed class OrderProcessingChangeConfiguration : IEntityTypeConfiguration<OrderProcessingChange>
{
    public void Configure(EntityTypeBuilder<OrderProcessingChange> b)
    {
        b.ToTable("OrderProcessingChanges");
        b.HasKey(x => new { x.OrderId, x.OperationId });
        b.HasIndex(x => new { x.OrderId, x.Version }).IsUnique();
        b.Property(x => x.FromStatus).HasMaxLength(20);
        b.Property(x => x.ToStatus).HasMaxLength(20);
        b.Property(x => x.Reason).HasMaxLength(500);
        b.Property(x => x.Actor).HasMaxLength(200);
        b.HasOne<DemoOrder>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
    }
}
