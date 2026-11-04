using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Infrastructure;
using Xunit;
namespace SurubDirect.Tests;
public sealed class AdminReturnFilterTests(OrderingCartFixture fixture) : IClassFixture<OrderingCartFixture>
{
    [Fact]
    public async Task Return_filter_is_applied_before_count_and_pagination_and_combines_with_existing_filters()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var now = DateTimeOffset.UtcNow;
        foreach (var state in Enumerable.Repeat("Requested", 31).Concat(new[] { "Approved", "Rejected", "Restoring", "Restored", "NotRequested" }))
        {
            var (order, plan) = ReturnSamples.Create();
            if (state != "NotRequested") order.ChangeReturn(Guid.NewGuid(), 0, "Requested", "Test return", "admin", now);
            if (state == "Rejected") order.ChangeReturn(Guid.NewGuid(), 1, "Rejected", "Test return", "admin", now);
            if (state is "Approved" or "Restoring" or "Restored") order.ChangeReturn(Guid.NewGuid(), 1, "Approved", "Test return", "admin", now);
            if (state is "Restoring" or "Restored") order.ChangeReturn(Guid.NewGuid(), 2, "Restoring", "Test return", "admin", now);
            if (state == "Restored") { plan.Restore(now); plan.ObserveOutcome("Restored", now, now); order.Reconcile(plan, now); }
            db.DemoOrders.Add(order);
        }
        await db.SaveChangesAsync();
        var handler = scope.ServiceProvider.GetRequiredService<ListAdminOrdersHandler>();
        var first = await handler.Handle(null, null, 1, default, " Requested ");
        var second = await handler.Handle(null, null, 2, default, "Requested");
        Assert.Equal(31, first.Total); Assert.Equal(31, second.Total);
        Assert.Equal(25, first.Items.Count); Assert.Equal(6, second.Items.Count);
        Assert.Empty(first.Items.Select(x => x.Id).Intersect(second.Items.Select(x => x.Id)));
        Assert.All(first.Items.Concat(second.Items), x => Assert.Equal("Requested", x.ReturnStatus));
        var numbers = first.Items.Concat(second.Items).Select(x => long.Parse(x.Number[5..])).ToArray();
        Assert.Equal(numbers.OrderDescending(), numbers);
        foreach (var state in new[] { "Approved", "Rejected", "Restoring", "Restored", "NotRequested" })
            Assert.Equal(state, Assert.Single((await handler.Handle(null, null, 1, default, state)).Items).ReturnStatus);
        var number = first.Items[0].Number;
        Assert.Single((await handler.Handle("Confirmed", number, 1, default, "Requested")).Items);
        Assert.Empty((await handler.Handle("Failed", number, 1, default, "Requested")).Items);
        Assert.Empty((await handler.Handle(null, number, 1, default, "Approved")).Items);
        Assert.Empty((await handler.Handle(null, null, 3, default, "Requested")).Items);
        Assert.Equal(36, (await handler.Handle(null, null, 1, default, " ")).Total);
    }
    [Theory]
    [InlineData("requested")]
    [InlineData("invalid")]
    [InlineData("Requested,Approved")]
    public async Task Invalid_return_filters_are_rejected(string status)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ListAdminOrdersHandler>();
        Assert.Equal(400, (await Assert.ThrowsAsync<BusinessException>(() => handler.Handle(null, null, 1, default, status))).Status);
    }
}
