using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Domain;
using SurubDirect.Ordering.Infrastructure;
using Xunit;
namespace SurubDirect.Tests;
public sealed class AccountPartialReturnTests(OrderingCartFixture fixture) : IClassFixture<OrderingCartFixture>
{
    private async Task<DemoOrder> Seed(string? account)
    {
        var (order, plan) = ReturnSamples.Create();
        if (account is not null) order.LinkAccount(account, DateTimeOffset.UtcNow);
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        db.DemoOrders.Add(order); db.ReservationPlans.Add(plan); await db.SaveChangesAsync();
        return order;
    }
    [Fact]
    public async Task Owner_can_request_and_replay_after_approval_without_exposing_admin_audit()
    {
        var order = await Seed("owner"); var id = Guid.NewGuid();
        await using var scope = fixture.Services.CreateAsyncScope();
        var customer = scope.ServiceProvider.GetRequiredService<IAccountPartialReturns>();
        var request = await customer.Request("owner", order.Id, id, [new(order.Lines[0].OfferId, 1)], "Motiv client", default);
        Assert.Equal("customer:owner", request.Actor);
        var admin = scope.ServiceProvider.GetRequiredService<IPartialReturns>();
        await admin.Decide(order.Id, id, Guid.NewGuid(), 0, "Approved", "Private admin reason", "private-operator", default);
        Assert.Equal("Approved", (await customer.Request("owner", order.Id, id, [new(order.Lines[0].OfferId, 1)], "Motiv client", default)).Status);
        var summary = await customer.Read("owner", order.Id, default);
        Assert.True(summary.CanRequest);
        Assert.Equal(1, Assert.Single(summary.Offers).AvailablePacks);
        Assert.Equal("Approved", Assert.Single(summary.Items).Status);
        var json = JsonSerializer.Serialize(summary);
        Assert.DoesNotContain("private", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("actor", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("decision", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ReceiptError", json);
    }
    [Fact]
    public async Task Ownership_is_checked_before_replay_and_unclaimed_orders_are_hidden()
    {
        var order = await Seed("owner"); var unclaimed = await Seed(null); var id = Guid.NewGuid();
        await using var scope = fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IAccountPartialReturns>();
        await repo.Request("owner", order.Id, id, [new(order.Lines[0].OfferId, 1)], "Retur client", default);
        foreach (var item in new[] { order, unclaimed })
        {
            Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => repo.Read("other", item.Id, default))).Status);
            Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => repo.Request("other", item.Id, id, [new(item.Lines[0].OfferId, 1)], "Retur client", default))).Status);
        }
    }
    [Fact]
    public async Task Admin_and_customer_share_capacity_and_rejection_releases_it()
    {
        var order = await Seed("owner"); var id = Guid.NewGuid();
        await using var scope = fixture.Services.CreateAsyncScope();
        var admin = scope.ServiceProvider.GetRequiredService<IPartialReturns>();
        var customer = scope.ServiceProvider.GetRequiredService<IAccountPartialReturns>();
        await admin.Request(order.Id, id, [new(order.Lines[0].OfferId, 2)], "Cerere administrativă", "admin", default);
        Assert.False((await customer.Read("owner", order.Id, default)).CanRequest);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => customer.Request("owner", order.Id, Guid.NewGuid(), [new(order.Lines[0].OfferId, 1)], "Cerere client", default))).Status);
        await admin.Decide(order.Id, id, Guid.NewGuid(), 0, "Rejected", "Cerere respinsă", "admin", default);
        Assert.Equal(2, Assert.Single((await customer.Read("owner", order.Id, default)).Offers).AvailablePacks);
        await customer.Request("owner", order.Id, Guid.NewGuid(), [new(order.Lines[0].OfferId, 2)], "Cerere client", default);
        Assert.False((await customer.Read("owner", order.Id, default)).CanRequest);
    }
    [Fact]
    public async Task Integral_request_disables_partial_creation_and_unknown_orders_are_not_empty_lists()
    {
        var order = await Seed("owner");
        await using var scope = fixture.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IOrderReturns>().Change(order.Id, Guid.NewGuid(), 0, "Requested", "Retur integral", "admin", default);
        var repo = scope.ServiceProvider.GetRequiredService<IAccountPartialReturns>();
        Assert.False((await repo.Read("owner", order.Id, default)).CanRequest);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => repo.Request("owner", order.Id, Guid.NewGuid(), [new(order.Lines[0].OfferId, 1)], "Retur parțial", default))).Status);
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => repo.Read("owner", Guid.NewGuid(), default))).Status);
    }
}
