using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurubDirect.Ordering.Domain;

namespace SurubDirect.Ordering.Infrastructure;
internal sealed class DemoOrderConfiguration : IEntityTypeConfiguration<DemoOrder>
{
    public void Configure(EntityTypeBuilder<DemoOrder> b)
    {
        b.ToTable("DemoOrders");
        b.Property(x => x.CancellationReason).HasMaxLength(500);
        b.Property(x => x.CancelledBy).HasMaxLength(200);
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.CartId);
        b.Property(x => x.AccountId).HasMaxLength(450);
        b.HasIndex(x => new { x.AccountId, x.Sequence });
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Sequence).UseIdentityAlwaysColumn();
        b.HasIndex(x => x.Sequence).IsUnique();
        b.Property(x => x.OwnerHash).HasMaxLength(64);
        b.Property(x => x.Status).HasMaxLength(20);
        b.HasIndex(x => new { x.OwnerHash, x.Sequence });
        b.Property(x => x.Currency).HasMaxLength(10);
        b.Property(x => x.Customer).HasColumnType("jsonb").HasConversion(v => JsonSerializer.Serialize(v, (JsonSerializerOptions? )null), v => JsonSerializer.Deserialize<CustomerDraft>(v, (JsonSerializerOptions? )null)!);
        b.Property(x => x.Lines).HasColumnType("jsonb").HasConversion(v => JsonSerializer.Serialize(v, (JsonSerializerOptions? )null), v => JsonSerializer.Deserialize<List<PreparedCartLine>>(v, (JsonSerializerOptions? )null)!).Metadata.SetValueComparer(new ValueComparer<List<PreparedCartLine>>((a, b) => a!.SequenceEqual(b!), v => v.Aggregate(0, (h, x) => HashCode.Combine(h, x.GetHashCode())), v => v.ToList()));
    }
}
