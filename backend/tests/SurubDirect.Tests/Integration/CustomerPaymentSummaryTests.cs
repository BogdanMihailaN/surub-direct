using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Infrastructure;
using Xunit;
namespace SurubDirect.Tests;
public sealed class CustomerPaymentSummaryTests(OrderingCartFixture fixture) : IClassFixture<OrderingCartFixture>
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task History_exposes_only_payment_summary_and_preserves_ownership(bool onAccount)
    {
        var (order, plan) = PaymentSimulationSamples.Create();
        var account = Guid.NewGuid().ToString();
        if (onAccount) order.LinkAccount(account, DateTimeOffset.UtcNow);
        await using var scope = fixture.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<OrderingDbContext>();
        db.Add(order); db.Add(plan); await db.SaveChangesAsync();
        var anonymous = services.GetRequiredService<IDemoOrders>();
        var accounts = services.GetRequiredService<IAccountOrders>();
        async Task<SurubDirect.Ordering.Contracts.DemoOrderDto> Read()
        {
            var page = onAccount ? await accounts.Read(account, 1, default) : await anonymous.Read(order.OwnerHash, 1, default);
            return Assert.Single(page.Items, x => x.Id == order.Id);
        }
        Assert.Equal("NotStarted", (await Read()).PaymentSummary.Status);
        var handler = services.GetRequiredService<SimulatePaymentHandler>();
        long version = 0;
        foreach (var (action, status) in new[] { ("Start", "Pending"), ("Fail", "Failed"), ("Start", "Pending"), ("Succeed", "Paid"), ("Refund", "Refunded") })
        {
            await handler.Handle(order.Id, Guid.NewGuid(), version++, action, "Private audit reason", "private-operator", default);
            var item = await Read();
            Assert.Equal(status, item.PaymentSummary.Status);
            Assert.Equal(44.2m, item.PaymentSummary.Amount);
            Assert.Equal("RON", item.PaymentSummary.Currency);
            var json = JsonSerializer.Serialize(item);
            Assert.DoesNotContain("Private audit reason", json);
            Assert.DoesNotContain("private-operator", json);
            using var summary = JsonDocument.Parse(JsonSerializer.Serialize(item.PaymentSummary));
            Assert.Equal(new[] { "Amount", "Currency", "RefundedAmount", "Status" }, summary.RootElement.EnumerateObject().Select(x => x.Name).Order().ToArray());
        }
        Assert.DoesNotContain((await accounts.Read("unrelated-account", 1, default)).Items, x => x.Id == order.Id);
        Assert.DoesNotContain((await anonymous.Read(new string('b', 64), 1, default)).Items, x => x.Id == order.Id);
        if (onAccount) Assert.DoesNotContain((await anonymous.Read(order.OwnerHash, 1, default)).Items, x => x.Id == order.Id);
        var cancelled = await services.GetRequiredService<IOrderCancellations>().Cancel(order.Id, "Test cancellation", "admin", default);
        Assert.Equal("Refunded", cancelled.PaymentSummary.Status);
        Assert.Equal("Refunded", (await Read()).PaymentSummary.Status);
    }
}
