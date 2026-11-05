using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Domain;
using Xunit;
namespace SurubDirect.Tests;
public sealed class PartialReturnReceptionTests
{
    private static PartialReturnRequest Create()
    {
        var (order, _) = ReturnSamples.Create();
        return PartialReturnRequest.Create(Guid.NewGuid(), order, [new(order.Lines[0].OfferId, 1)], "Retur produse", "admin", DateTimeOffset.UtcNow);
    }
    [Fact]
    public void Reception_requires_approval_current_version_and_explicit_confirmation()
    {
        var r = Create(); var now = DateTimeOffset.UtcNow;
        Assert.Throws<BusinessException>(() => r.Receive(Guid.NewGuid(), 0, "Produse primite", "admin", true, now));
        r.Decide(Guid.NewGuid(), 0, "Approved", "Aprobare retur", "admin", now);
        Assert.Throws<BusinessException>(() => r.Receive(Guid.NewGuid(), 1, "Produse primite", "admin", false, now));
        Assert.Throws<BusinessException>(() => r.Receive(Guid.NewGuid(), 0, "Produse primite", "admin", true, now));
        Assert.Equal("Approved", r.Status);
    }
    [Fact]
    public void Receipt_and_decision_replays_remain_valid_after_completion_but_changed_content_is_rejected()
    {
        var r = Create(); var now = DateTimeOffset.UtcNow; var decision = Guid.NewGuid(); var receipt = Guid.NewGuid();
        r.Decide(decision, 0, "Approved", "Aprobare retur", "admin", now);
        r.Receive(receipt, 1, "Produse primite", "admin", true, now);
        r.ConfirmReceipt(now);
        r.Receive(receipt, 1, "Produse primite", "admin", true, now.AddDays(1));
        r.Decide(decision, 0, "Approved", "Aprobare retur", "admin", now.AddDays(1));
        Assert.Equal("Restored", r.Status); Assert.Equal(2, r.Version);
        Assert.Throws<BusinessException>(() => r.Receive(receipt, 1, "Alt motiv", "admin", true, now));
        Assert.Throws<BusinessException>(() => r.Receive(Guid.NewGuid(), 2, "Produse primite", "admin", true, now));
        Assert.Throws<BusinessException>(() => r.Decide(decision, 0, "Rejected", "Aprobare retur", "admin", now));
    }
}
