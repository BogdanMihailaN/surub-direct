using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurubDirect.Ordering.Domain;

namespace SurubDirect.Ordering.Infrastructure;
internal sealed class ReservationPlanConfiguration : IEntityTypeConfiguration<ReservationPlan>
{
    public void Configure(EntityTypeBuilder<ReservationPlan> b)
    {
        b.ToTable("ReservationPlans");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Ignore(x => x.Terminal);
        b.Property(x => x.Status).HasMaxLength(20);
        b.Property(x => x.LastError).HasMaxLength(100);
        b.HasIndex(x => new { x.Status, x.NextAttemptAt });
        b.Property(x => x.Lines).HasColumnType("jsonb").HasConversion(v => JsonSerializer.Serialize(v, (JsonSerializerOptions? )null), v => JsonSerializer.Deserialize<List<ReservationItem>>(v, (JsonSerializerOptions? )null)!).Metadata.SetValueComparer(new ValueComparer<List<ReservationItem>>((a, b) => a!.SequenceEqual(b!), v => v.Aggregate(0, (h, x) => HashCode.Combine(h, x.GetHashCode())), v => v.ToList()));
    }
}
