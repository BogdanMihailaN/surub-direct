using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurubDirect.Ordering.Domain;
namespace SurubDirect.Ordering.Infrastructure;
internal sealed class PaymentSimulationEventConfiguration : IEntityTypeConfiguration<PaymentSimulationEvent>
{
    public void Configure(EntityTypeBuilder<PaymentSimulationEvent> b)
    {
        b.ToTable("PaymentSimulationEvents");
        b.Property(x => x.RefundAmount).HasPrecision(18, 2);
        b.HasIndex(x => new { x.OrderId, x.PartialReturnId }).IsUnique();
        b.HasKey(x => new { x.OrderId, x.OperationId });
        b.HasIndex(x => new { x.OrderId, x.Version }).IsUnique();
        b.Property(x => x.Action).HasMaxLength(20);
        b.Property(x => x.FromStatus).HasMaxLength(20);
        b.Property(x => x.ToStatus).HasMaxLength(20);
        b.Property(x => x.Reason).HasMaxLength(500);
        b.Property(x => x.Actor).HasMaxLength(200);
        b.HasOne<PaymentSimulation>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
    }
}
