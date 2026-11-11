using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Domain;
using Xunit;
namespace SurubDirect.Tests;
public sealed class PartialReturnReceiptRetryTests
{
    [Fact]
    public void Retry_requires_failed_receipt_current_version_reason_and_actor()
    {
        var (order, _) = ReturnSamples.Create();
        var now = DateTimeOffset.UtcNow;
        var r = PartialReturnRequest.Create(Guid.NewGuid(), order, [new(order.Lines[0].OfferId, 1)], "Return", "admin", now);
        Assert.Throws<BusinessException>(() => r.RetryReceipt(Guid.NewGuid(), 0, "Retry", "admin", now));
        r.Decide(Guid.NewGuid(), 0, "Approved", "Approve", "admin", now);
        r.Receive(Guid.NewGuid(), 1, "Received", "admin", true, now);
        Assert.Throws<BusinessException>(() => r.RetryReceipt(Guid.NewGuid(), 2, "Retry", "admin", now));
        r.ReceiptFailed(now);
        Assert.Throws<BusinessException>(() => r.RetryReceipt(Guid.Empty, 2, "Retry", "admin", now));
        Assert.Throws<BusinessException>(() => r.RetryReceipt(Guid.NewGuid(), 2, " ", "admin", now));
        Assert.Throws<BusinessException>(() => r.RetryReceipt(Guid.NewGuid(), 2, "Retry", "", now));
        Assert.Throws<BusinessException>(() => r.RetryReceipt(Guid.NewGuid(), 1, "Retry", "admin", now));
        Assert.Empty(r.ReceiptRetries);
        Assert.Equal(2, r.Version);
    }
    [Fact]
    public void Retry_preserves_intent_limits_frequency_and_remains_replayable_after_completion()
    {
        var (order, _) = ReturnSamples.Create();
        var now = DateTimeOffset.UtcNow;
        var r = PartialReturnRequest.Create(Guid.NewGuid(), order, [new(order.Lines[0].OfferId, 1)], "Return", "admin", now);
        r.Decide(Guid.NewGuid(), 0, "Approved", "Approve", "admin", now);
        var receiptId = Guid.NewGuid();
        r.Receive(receiptId, 1, "Received", "admin", true, now);
        r.ReceiptFailed(now);
        var id = Guid.NewGuid();
        r.RetryReceipt(id, 2, " Retry ", "admin", now);
        Assert.Throws<BusinessException>(() => r.RetryReceipt(Guid.NewGuid(), 3, "Retry", "admin", now.AddSeconds(59)));
        r.RetryReceipt(Guid.NewGuid(), 3, "Retry", "admin", now.AddMinutes(1));
        Assert.Equal(receiptId, r.ReceiptOperationId);
        Assert.Equal(1, r.ReceiptAttempts);
        Assert.Equal(2, r.ReceiptRetries.Count);
        r.ConfirmReceipt(now.AddMinutes(1));
        r.RetryReceipt(id, 2, "Retry", "admin", now.AddMinutes(2));
        Assert.Throws<BusinessException>(() => r.RetryReceipt(id, 2, "Retry", "other", now));
        Assert.Throws<BusinessException>(() => r.RetryReceipt(Guid.NewGuid(), 4, "Retry", "admin", now.AddMinutes(3)));
        Assert.Null(r.NextReceiptAttemptAt);
        Assert.Equal("Restored", r.Status);
    }
}
