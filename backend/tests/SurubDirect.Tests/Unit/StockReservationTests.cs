using SurubDirect.BuildingBlocks;
using SurubDirect.Inventory.Domain;
using Xunit;

namespace SurubDirect.Tests.Unit;
public sealed class StockReservationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(59)]
    [InlineData(1801)]
    public void Rejects_invalid_duration(int duration) => Assert.Throws<BusinessException>(() => StockReservation.Create(Guid.NewGuid(), Guid.NewGuid(), duration, [new(Guid.NewGuid(), 1)], DateTimeOffset.UtcNow));
    [Fact]
    public void Rejects_invalid_lines_and_ids()
    {
        var line = new ReservationLine(Guid.NewGuid(), 1);
        void Reject(IReadOnlyList<ReservationLine>? lines) => Assert.Throws<BusinessException>(() => StockReservation.Create(Guid.NewGuid(), Guid.NewGuid(), 60, lines, DateTimeOffset.UtcNow));
        Reject(null);
        Reject([]);
        Reject([line, line]);
        Reject([line with { Quantity = 0 }]);
        Reject([line with { VariantId = Guid.Empty }]);
        Reject(Enumerable.Range(0, 51).Select(_ => new ReservationLine(Guid.NewGuid(), 1)).ToList());
        Assert.Throws<BusinessException>(() => StockReservation.CancelBeforeCreation(Guid.Empty, Guid.NewGuid(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void End_is_terminal_and_expiry_boundary_is_inclusive()
    {
        var now = DateTimeOffset.UtcNow;
        var reservation = StockReservation.Create(Guid.NewGuid(), Guid.NewGuid(), 60, [new(Guid.NewGuid(), 1)], now);
        Assert.True(reservation.End(now.AddSeconds(60)));
        Assert.Equal(ReservationStatus.Expired, reservation.Status);
        Assert.False(reservation.End(now.AddSeconds(61)));
        Assert.Equal(now.AddSeconds(60), reservation.EndedAt);
    }

    [Fact]
    public void Consume_rejects_exact_expiry_boundary_and_is_idempotent_when_successful()
    {
        var now = DateTimeOffset.UtcNow;
        var r = StockReservation.Create(Guid.NewGuid(), Guid.NewGuid(), 60, [new(Guid.NewGuid(), 1)], now);
        Assert.Throws<BusinessException>(() => r.Consume(now.AddSeconds(60)));
        Assert.Equal(ReservationStatus.Active, r.Status);
        Assert.True(r.Consume(now.AddSeconds(59)));
        Assert.False(r.Consume(now.AddHours(1)));
        Assert.False(r.End(now.AddHours(1)));
        Assert.Equal(ReservationStatus.Consumed, r.Status);
        Assert.Equal(now.AddSeconds(59), r.EndedAt);
    }
}
