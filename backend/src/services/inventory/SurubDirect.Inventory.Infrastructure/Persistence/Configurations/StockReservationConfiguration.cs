using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurubDirect.Inventory.Domain;

namespace SurubDirect.Inventory.Infrastructure;
internal sealed class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> b)
    {
        b.ToTable("Reservations");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => new { x.Status, x.ExpiresAt });
        b.HasIndex(x => x.OrderId);
        b.Property(x => x.Lines).HasColumnType("jsonb").HasConversion(v => JsonSerializer.Serialize(v, (JsonSerializerOptions? )null), v => JsonSerializer.Deserialize<List<ReservationLine>>(v, (JsonSerializerOptions? )null)!).Metadata.SetValueComparer(new ValueComparer<List<ReservationLine>>((a, b) => a!.SequenceEqual(b!), v => v.Aggregate(0, (h, x) => HashCode.Combine(h, x.GetHashCode())), v => v.ToList()));
    }
}
