using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Domain;
using SurubDirect.Ordering.Infrastructure;
using Xunit;

namespace SurubDirect.Tests;

public sealed class PartialReturnRefundTests(OrderingCartFixture fixture) : IClassFixture<OrderingCartFixture>
{
    private async Task<(Guid Order, Guid First, Guid Second)> Seed(bool restored = true)
    {
        var (order, plan) = ReturnSamples.Create();
        var now = DateTimeOffset.UtcNow;
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        db.ReservationPlans.Add(plan);
        db.DemoOrders.Add(order);
        var payment = PaymentSimulation.Create(order);
        db.PaymentSimulations.Add(payment);
        foreach (var action in new[] { "Start", "Succeed" })
            db.PaymentSimulationEvents.Add(payment.Apply(order.Status, Guid.NewGuid(), payment.Version, action, "Test plată", "admin", now));
        PartialReturnRequest Create()
        {
            var request = PartialReturnRequest.Create(Guid.NewGuid(), order, [new(order.Lines[0].OfferId, 1)], "Cerere retur", "admin", now);
            request.Decide(Guid.NewGuid(), 0, "Approved", "Cerere aprobată", "admin", now);
            request.Receive(Guid.NewGuid(), 1, "Produse primite", "admin", true, now);
            if (restored) request.ConfirmReceipt(now);
            db.PartialReturns.Add(request);
            return request;
        }
        var first = Create();
        var second = Create();
        await db.SaveChangesAsync();
        return (order.Id, first.Id, second.Id);
    }

    private async Task Refund(Guid order, Guid request, Guid operation, long version)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<RefundPartialReturnHandler>()
            .Handle(order, request, operation, version, "Produse restituite", "admin", default);
    }

    [Fact]
    public async Task Concurrent_replay_refunds_one_pack_once_and_full_refund_settles_only_remainder()
    {
        var (order, first, second) = await Seed();
        var operation = Guid.NewGuid();
        await Task.WhenAll(Refund(order, first, operation, 2), Refund(order, first, operation, 2));
        await using var scope = fixture.Services.CreateAsyncScope();
        var admin = scope.ServiceProvider.GetRequiredService<IAdminDemoOrders>();
        var detail = (await admin.Find(order, default))!;
        Assert.Equal(12.1m, detail.Order.PaymentSummary.RefundedAmount);
        Assert.Equal("PartiallyRefunded", detail.PaymentSimulation.Status);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => Refund(order, first, Guid.NewGuid(), 3))).Status);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => Refund(order, second, operation, 2))).Status);
        await Refund(order, second, Guid.NewGuid(), 3);
        await scope.ServiceProvider.GetRequiredService<IPaymentSimulations>().Apply(order, Guid.NewGuid(), 4, "Refund", "Sold transport", "admin", default);
        await Refund(order, first, operation, 2);
        detail = (await admin.Find(order, default))!;
        Assert.Equal("Refunded", detail.PaymentSimulation.Status);
        Assert.Equal(44.2m, detail.Order.PaymentSummary.RefundedAmount);
        Assert.Equal(20m, Assert.Single(detail.PaymentSimulation.History, e => e.Action == "Refund").RefundAmount);
        Assert.Equal(44.2m, detail.PaymentSimulation.History.Sum(e => e.RefundAmount));
        Assert.Equal("Consumed", detail.Reservation.Status);
    }

    [Fact]
    public async Task Full_and_partial_operations_on_same_payment_version_have_one_winner()
    {
        var (order, first, _) = await Seed();
        async Task<bool> Attempt(bool full)
        {
            try
            {
                if (full)
                {
                    await using var scope = fixture.Services.CreateAsyncScope();
                    await scope.ServiceProvider.GetRequiredService<IPaymentSimulations>()
                        .Apply(order, Guid.NewGuid(), 2, "Refund", "Rambursare integrală", "admin", default);
                }
                else
                {
                    await Refund(order, first, Guid.NewGuid(), 2);
                }
                return true;
            }
            catch (BusinessException ex) when (ex.Status == 409) { return false; }
        }
        Assert.Single(await Task.WhenAll(Attempt(true), Attempt(false)), success => success);
        await using var read = fixture.Services.CreateAsyncScope();
        var payment = (await read.ServiceProvider.GetRequiredService<IAdminDemoOrders>().Find(order, default))!.PaymentSimulation;
        Assert.Equal(3, payment.Version);
        Assert.Equal(payment.RefundedAmount, Assert.Single(payment.History, e => e.RefundAmount.HasValue).RefundAmount);
        Assert.InRange(payment.RefundedAmount, 12.1m, 44.2m);
    }

    [Fact]
    public async Task Receipt_in_progress_is_visible_with_retry_schedule_and_cannot_be_refunded()
    {
        var (order, first, _) = await Seed(false);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => Refund(order, first, Guid.NewGuid(), 2))).Status);
        await using var scope = fixture.Services.CreateAsyncScope();
        var admin = scope.ServiceProvider.GetRequiredService<IAdminDemoOrders>();
        var item = Assert.Single((await admin.Read(null, null, 1, default, partialReturnStatus: "Restoring")).Items, x => x.Id == order);
        Assert.Equal(2, item.PendingReceipts.Count);
        var receipt = Assert.Single(item.PendingReceipts, x => x.RequestId == first);
        Assert.NotNull(receipt.RequestedAt);
        Assert.NotNull(receipt.NextAttemptAt);
        Assert.Equal(0, (await admin.Find(order, default))!.PaymentSimulation.RefundedAmount);
    }
}
