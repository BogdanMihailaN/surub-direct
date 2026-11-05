using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Domain;
using SurubDirect.Ordering.Infrastructure;
using Xunit;
namespace SurubDirect.Tests;
public sealed class PartialReturnRequestTests(OrderingCartFixture fixture) : IClassFixture<OrderingCartFixture>
{
    private async Task<DemoOrder> Seed(bool delivered = true)
    {
        var (order, plan) = delivered ? ReturnSamples.Create() : PaymentSimulationSamples.Create();
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        db.DemoOrders.Add(order); db.ReservationPlans.Add(plan); await db.SaveChangesAsync();
        return order;
    }
    private async Task<PartialReturnRequest> Request(DemoOrder order, Guid id, int packs = 1)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IPartialReturns>().Request(order.Id, id, [new(order.Lines[0].OfferId, packs)], "Retur produse", "admin", default);
    }
    private async Task Decide(DemoOrder order, Guid id, Guid operation, string target, long version = 0)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IPartialReturns>().Decide(order.Id, id, operation, version, target, "Decizie verificată", "admin", default);
    }
    [Fact]
    public async Task Concurrent_replay_saves_one_snapshot_and_no_stock_or_payment_intent()
    {
        var order = await Seed(); var id = Guid.NewGuid();
        var results = await Task.WhenAll(Request(order, id), Request(order, id));
        Assert.Equal(results[0].CreatedAt, results[1].CreatedAt);
        Assert.Equal(10, Assert.Single(results[0].Lines).Quantity);
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        Assert.Single(await db.PartialReturns.Where(x => x.OrderId == order.Id).ToListAsync());
        var plan = await db.ReservationPlans.SingleAsync(x => x.Id == order.Id);
        Assert.Equal("Consumed", plan.Status); Assert.False(plan.RestorationRequested);
        Assert.False(await db.ReturnChanges.AnyAsync(x => x.OrderId == order.Id));
        Assert.False(await db.PaymentSimulations.AnyAsync(x => x.OrderId == order.Id));
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => Request(order, id, 2))).Status);
    }
    [Fact]
    public async Task Distinct_concurrent_requests_cannot_exceed_original_packs()
    {
        var order = await Seed();
        async Task<bool> Attempt()
        {
            try { await Request(order, Guid.NewGuid(), 2); return true; }
            catch (BusinessException e) { Assert.Equal(409, e.Status); return false; }
        }
        Assert.Single(await Task.WhenAll(Attempt(), Attempt()), x => x);
    }
    [Fact]
    public async Task Rejection_releases_selection_but_replay_preserves_rejected_request()
    {
        var order = await Seed(); var id = Guid.NewGuid();
        await Request(order, id, 2);
        var decision = Guid.NewGuid();
        await Decide(order, id, decision, "Rejected");
        await Decide(order, id, decision, "Rejected");
        Assert.Equal("Rejected", (await Request(order, id, 2)).Status);
        await Request(order, Guid.NewGuid(), 2);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => Decide(order, id, decision, "Approved"))).Status);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => Decide(order, id, Guid.NewGuid(), "Approved", 1))).Status);
    }
    [Fact]
    public async Task Concurrent_decisions_have_one_winner_and_approval_does_not_release_quantities()
    {
        var order = await Seed(); var id = Guid.NewGuid();
        await Request(order, id, 2);
        async Task<bool> Attempt()
        {
            try { await Decide(order, id, Guid.NewGuid(), "Approved"); return true; }
            catch (BusinessException e) { Assert.Equal(409, e.Status); return false; }
        }
        Assert.Single(await Task.WhenAll(Attempt(), Attempt()), x => x);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => Request(order, Guid.NewGuid()))).Status);
    }
    [Fact]
    public async Task Integral_and_partial_requests_are_serialized_and_mutually_exclusive()
    {
        var order = await Seed();
        async Task<bool> Integral()
        {
            await using var scope = fixture.Services.CreateAsyncScope();
            try { await scope.ServiceProvider.GetRequiredService<IOrderReturns>().Change(order.Id, Guid.NewGuid(), 0, "Requested", "Retur integral", "admin", default); return true; }
            catch (BusinessException e) { Assert.Equal(409, e.Status); return false; }
        }
        async Task<bool> Partial()
        {
            try { await Request(order, Guid.NewGuid()); return true; }
            catch (BusinessException e) { Assert.Equal(409, e.Status); return false; }
        }
        Assert.Single(await Task.WhenAll(Integral(), Partial()), x => x);
    }
    [Fact]
    public async Task Undelivered_orders_and_cross_order_receipt_identity_are_rejected()
    {
        var undelivered = await Seed(false);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => Request(undelivered, Guid.NewGuid()))).Status);
        var order = await Seed(); var other = await Seed(); var id = Guid.NewGuid();
        await Request(order, id);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => Request(other, id))).Status);
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => Decide(other, id, Guid.NewGuid(), "Approved"))).Status);
    }
    [Fact]
    public async Task Integral_request_is_allowed_after_all_partial_requests_were_rejected()
    {
        var order = await Seed(); var id = Guid.NewGuid();
        await Request(order, id);
        await Decide(order, id, Guid.NewGuid(), "Rejected");
        await using var scope = fixture.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IOrderReturns>().Change(order.Id, Guid.NewGuid(), 0, "Requested", "Retur integral", "admin", default);
        Assert.Equal("Rejected", (await Request(order, id)).Status);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => Request(order, Guid.NewGuid()))).Status);
    }
}
