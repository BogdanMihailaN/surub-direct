using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurubDirect.Ordering.Domain;
namespace SurubDirect.Ordering.Infrastructure;
internal sealed class PaymentSimulationConfiguration : IEntityTypeConfiguration<PaymentSimulation>
{
    public void Configure(EntityTypeBuilder<PaymentSimulation> b)
    {
        b.ToTable("PaymentSimulations");
        b.Property(x => x.PartialRefundTotal).HasPrecision(18, 2);
        b.Ignore(x => x.RefundedAmount);
        b.HasKey(x => x.OrderId);
        b.Property(x => x.OrderId).ValueGeneratedNever();
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.Currency).HasMaxLength(10);
        b.Property(x => x.Status).HasMaxLength(20);
        b.HasOne<DemoOrder>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
    }
}
