using SurubDirect.BuildingBlocks;
using Xunit;
namespace SurubDirect.Tests;
public sealed class OrderReturnTests
{
    [Fact]
    public void Return_requires_delivery_and_never_completes_before_inventory_confirms()
    {
        var (unshipped, _) = PaymentSimulationSamples.Create();
        var now = DateTimeOffset.UtcNow;
        Assert.Throws<BusinessException>(() => unshipped.ChangeReturn(Guid.NewGuid(), 0, "Requested", "Test return", "admin", now));
        var (order, plan) = ReturnSamples.Create();
        order.ChangeReturn(Guid.NewGuid(), 0, "Requested", "Test return", "admin", now);
        order.ChangeReturn(Guid.NewGuid(), 1, "Approved", "Test return", "admin", now);
        Assert.Equal("Consumed", plan.Status);
        order.ChangeReturn(Guid.NewGuid(), 2, "Restoring", "Received all", "admin", now);
        order.Reconcile(plan, now);
        Assert.Equal("Restoring", order.ReturnStatus);
        Assert.Null(order.ReturnRestoredAt);
        plan.Restore(now);
        plan.ObserveOutcome("Restored", now, now);
        order.Reconcile(plan, now);
        Assert.Equal("Restored", order.ReturnStatus);
        Assert.Equal(now, order.ReturnRestoredAt);
        order.Reconcile(plan, now.AddDays(1));
        Assert.Equal(now, order.ReturnRestoredAt);
        Assert.Equal("Confirmed", order.Status);
        Assert.Throws<BusinessException>(() => order.ChangeReturn(Guid.NewGuid(), 3, "Requested", "Another return", "admin", now));
    }
    [Theory]
    [InlineData(0, "Approved")]
    [InlineData(0, "Restoring")]
    [InlineData(1, "Requested")]
    public void Invalid_transitions_do_not_mutate_return(long version, string target)
    {
        var (order, _) = ReturnSamples.Create();
        Assert.Throws<BusinessException>(() => order.ChangeReturn(Guid.NewGuid(), version, target, "Test return", "admin", DateTimeOffset.UtcNow));
        Assert.Equal("NotRequested", order.ReturnStatus);
        Assert.Equal(0, order.ReturnVersion);
    }
}
