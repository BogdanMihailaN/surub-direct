using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Domain;
using SurubDirect.Ordering.Infrastructure;
using Xunit;
namespace SurubDirect.Tests;
public sealed class AdminPaymentFilterTests(OrderingCartFixture fixture) : IClassFixture<OrderingCartFixture>
{
    [Fact]
    public async Task Payment_filter_includes_unstarted_orders_and_combines_with_returns_before_pagination()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var now = DateTimeOffset.UtcNow;
        foreach (var state in Enumerable.Repeat("Paid", 31).Concat(new[] { "Pending", "Failed", "Refunded", "NotStarted", "Absent" }))
        {
            var (order, _) = ReturnSamples.Create();
            if (state == "Paid") order.ChangeReturn(Guid.NewGuid(), 0, "Requested", "Test return", "admin", now);
            db.DemoOrders.Add(order);
            if (state == "Absent") continue;
            var payment = PaymentSimulation.Create(order);
            if (state != "NotStarted") payment.Apply("Confirmed", Guid.NewGuid(), payment.Version, "Start", "Test payment", "admin", now);
            if (state is "Paid" or "Refunded") payment.Apply("Confirmed", Guid.NewGuid(), payment.Version, "Succeed", "Test payment", "admin", now);
            if (state == "Failed") payment.Apply("Confirmed", Guid.NewGuid(), payment.Version, "Fail", "Test payment", "admin", now);
            if (state == "Refunded") payment.Apply("Confirmed", Guid.NewGuid(), payment.Version, "Refund", "Test payment", "admin", now);
            db.PaymentSimulations.Add(payment);
        }
        await db.SaveChangesAsync();
        var handler = scope.ServiceProvider.GetRequiredService<ListAdminOrdersHandler>();
        var first = await handler.Handle(null, null, 1, default, "Requested", " Paid ");
        var second = await handler.Handle(null, null, 2, default, "Requested", "Paid");
        Assert.Equal(31, first.Total); Assert.Equal(31, second.Total);
        Assert.Equal(25, first.Items.Count); Assert.Equal(6, second.Items.Count);
        Assert.Empty(first.Items.Select(x => x.Id).Intersect(second.Items.Select(x => x.Id)));
        Assert.All(first.Items.Concat(second.Items), x => Assert.Equal("Paid", x.PaymentStatus));
        var numbers = first.Items.Concat(second.Items).Select(x => long.Parse(x.Number[5..])).ToArray();
        Assert.Equal(numbers.OrderDescending(), numbers);
        foreach (var state in new[] { "Pending", "Failed", "Refunded" })
            Assert.Equal(state, Assert.Single((await handler.Handle(null, null, 1, default, paymentStatus: state)).Items).PaymentStatus);
        var unstarted = await handler.Handle(null, null, 1, default, paymentStatus: "NotStarted");
        Assert.Equal(2, unstarted.Total);
        Assert.All(unstarted.Items, x => Assert.Equal("NotStarted", x.PaymentStatus));
        var number = first.Items[0].Number;
        Assert.Single((await handler.Handle("Confirmed", number, 1, default, "Requested", "Paid")).Items);
        Assert.Empty((await handler.Handle("Failed", number, 1, default, "Requested", "Paid")).Items);
        Assert.Empty((await handler.Handle(null, number, 1, default, "Restored", "Paid")).Items);
        Assert.Empty((await handler.Handle(null, number, 1, default, "Requested", "Refunded")).Items);
        Assert.Empty((await handler.Handle(null, null, 3, default, paymentStatus: "Paid")).Items);
        Assert.Equal(36, (await handler.Handle(null, null, 1, default, paymentStatus: " ")).Total);
    }
    [Theory]
    [InlineData("paid")]
    [InlineData("invalid")]
    [InlineData("Paid,Refunded")]
    public async Task Invalid_payment_filters_are_rejected(string status)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ListAdminOrdersHandler>();
        Assert.Equal(400, (await Assert.ThrowsAsync<BusinessException>(() => handler.Handle(null, null, 1, default, paymentStatus: status))).Status);
    }
}
