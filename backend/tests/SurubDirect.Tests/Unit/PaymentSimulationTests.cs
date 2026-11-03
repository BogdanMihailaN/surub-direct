using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Domain;
using Xunit;
namespace SurubDirect.Tests;
public sealed class PaymentSimulationTests
{
    [Fact]
    public void Retry_has_a_new_attempt_and_refund_is_terminal_for_the_original_total()
    {
        var (order, _) = PaymentSimulationSamples.Create();
        var payment = PaymentSimulation.Create(order);
        Assert.Equal(44.2m, payment.Amount);
        Assert.Equal("RON", payment.Currency);
        var now = DateTimeOffset.UtcNow;
        var start = payment.Apply("Confirmed", Guid.NewGuid(), 0, "Start", "Test local", "admin", now);
        Assert.Throws<BusinessException>(payment.EnsureCancellationAllowed);
        payment.Apply("Confirmed", Guid.NewGuid(), 1, "Fail", "Test local", "admin", now);
        payment.EnsureCancellationAllowed();
        var retry = payment.Apply("Confirmed", Guid.NewGuid(), 2, "Start", "Test local", "admin", now);
        Assert.NotEqual(start.AttemptId, retry.AttemptId);
        payment.Apply("Confirmed", Guid.NewGuid(), 3, "Succeed", "Test local", "admin", now);
        Assert.Throws<BusinessException>(payment.EnsureCancellationAllowed);
        var refund = payment.Apply("Confirmed", Guid.NewGuid(), 4, "Refund", "Test local", "admin", now);
        Assert.Equal(retry.AttemptId, refund.AttemptId);
        payment.EnsureCancellationAllowed();
        Assert.Equal("Refunded", payment.Status);
        Assert.Equal(44.2m, payment.Amount);
        Assert.Throws<BusinessException>(() => payment.Apply("Confirmed", Guid.NewGuid(), 5, "Start", "Test local", "admin", now));
    }

    [Theory]
    [InlineData("Confirmed", "Succeed", 0)]
    [InlineData("Confirmed", "Refund", 0)]
    [InlineData("Confirmed", "Start", 1)]
    [InlineData("Confirming", "Start", 0)]
    [InlineData("Cancelled", "Start", 0)]
    public void Invalid_transitions_leave_the_simulation_untouched(string status, string action, long version)
    {
        var payment = PaymentSimulation.Create(PaymentSimulationSamples.Create().Order);
        Assert.Throws<BusinessException>(() => payment.Apply(status, Guid.NewGuid(), version, action, "Test local", "admin", DateTimeOffset.UtcNow));
        Assert.Equal(0, payment.Version);
        Assert.Equal("NotStarted", payment.Status);
        Assert.Null(payment.AttemptId);
    }
}
