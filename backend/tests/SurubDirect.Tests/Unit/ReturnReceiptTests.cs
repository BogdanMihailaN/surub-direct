using SurubDirect.BuildingBlocks;
using SurubDirect.Inventory.Domain;
using Xunit;
namespace SurubDirect.Tests;
public sealed class ReturnReceiptTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Non_positive_quantities_are_rejected(int quantity) => Assert.Throws<BusinessException>(() =>
        ReturnReceipt.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), [new(Guid.NewGuid(), quantity)], DateTimeOffset.UtcNow));
    [Fact]
    public void Empty_duplicate_null_and_overlarge_selections_are_rejected()
    {
        var line = new ReturnReceiptLine(Guid.NewGuid(), 1);
        foreach (var lines in new IEnumerable<ReturnReceiptLine>?[] { null, [], [line, line], [null!], [new(Guid.Empty, 1)], Enumerable.Range(0, 101).Select(_ => new ReturnReceiptLine(Guid.NewGuid(), 1)) })
            Assert.Throws<BusinessException>(() => ReturnReceipt.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), lines, DateTimeOffset.UtcNow));
        Assert.Throws<BusinessException>(() => ReturnReceipt.Create(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), [line], DateTimeOffset.UtcNow));
    }
    [Fact]
    public void Line_order_and_new_request_time_do_not_change_replay_identity()
    {
        var lines = new[] { new ReturnReceiptLine(Guid.NewGuid(), 1), new ReturnReceiptLine(Guid.NewGuid(), 2) };
        var receipt = ReturnReceipt.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), lines, DateTimeOffset.UtcNow);
        receipt.EnsureSame(ReturnReceipt.Create(receipt.Id, receipt.ReservationId, receipt.OrderId, lines.Reverse(), DateTimeOffset.UtcNow.AddMinutes(1)));
    }
}
