using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Domain;
using Xunit;

namespace SurubDirect.Tests.Unit;
public sealed class ReservationConfirmationTests
{
    [Theory]
    [InlineData("Pending")]
    [InlineData("Cancelling")]
    [InlineData("Released")]
    [InlineData("Expired")]
    [InlineData("Consumed")]
    public void Only_active_reservations_can_start_confirmation(string status)
    {
        var now = DateTimeOffset.UtcNow;
        var plan = ReservationPlan.Create(Guid.NewGuid(), Guid.NewGuid(), [new(Guid.NewGuid(), 1)], now);
        plan.Observe(status, now.AddMinutes(15), now);
        Assert.Throws<BusinessException>(() => plan.Confirm(now));
        Assert.False(plan.ConfirmationRequested);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Confirmation_checks_both_deadlines_inclusively(bool localDeadline)
    {
        var now = DateTimeOffset.UtcNow;
        var plan = ReservationPlan.Create(Guid.NewGuid(), Guid.NewGuid(), [new(Guid.NewGuid(), 1)], localDeadline ? now.AddMinutes(-15) : now);
        plan.Observe("Active", localDeadline ? now.AddMinutes(15) : now, now);
        Assert.Throws<BusinessException>(() => plan.Confirm(now));
        Assert.False(plan.ConfirmationRequested);
    }
}
