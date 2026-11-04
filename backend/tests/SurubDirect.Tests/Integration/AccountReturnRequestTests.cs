using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Infrastructure;
using Xunit;
namespace SurubDirect.Tests;
public sealed class AccountReturnRequestTests(OrderingCartFixture fixture) : IClassFixture<OrderingCartFixture>
{
    private async Task<Guid> Seed(string? account, bool delivered = true)
    {
        var (order, plan) = delivered ? ReturnSamples.Create() : PaymentSimulationSamples.Create();
        if (account is not null) order.LinkAccount(account, DateTimeOffset.UtcNow);
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        db.Add(order); db.Add(plan); await db.SaveChangesAsync(); return order.Id;
    }
    private async Task Request(string account, Guid id, Guid operation, string reason = "Retur integral solicitat")
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<RequestAccountReturnHandler>().Handle(account, id, operation, reason, default);
    }
    [Fact]
    public async Task Owner_request_is_idempotent_and_replay_after_admin_approval_does_not_reopen_or_restock()
    {
        var account = Guid.NewGuid().ToString(); var id = await Seed(account); var operation = Guid.NewGuid();
        await Task.WhenAll(Request(account, id, operation), Request(account, id, operation));
        await using var scope = fixture.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        await services.GetRequiredService<ChangeReturnHandler>().Handle(id, Guid.NewGuid(), 1, "Approved", "Accept integral", "admin", false, default);
        await Request(account, id, operation);
        await Assert.ThrowsAsync<BusinessException>(() => Request(account, id, operation, "Alt motiv pentru aceeași operație"));
        await Assert.ThrowsAsync<BusinessException>(() => Request(account, id, Guid.NewGuid()));
        var detail = (await services.GetRequiredService<IAdminDemoOrders>().Find(id, default))!;
        Assert.Equal("Approved", detail.Return.Status);
        Assert.Equal(2, detail.Return.History.Count);
        Assert.Equal("customer:" + account, detail.Return.History[0].Actor);
        Assert.Equal("Consumed", detail.Reservation.Status);
        Assert.Equal("NotStarted", detail.PaymentSimulation.Status);
        var page = await services.GetRequiredService<IAccountOrders>().Read(account, 1, default);
        Assert.Equal("Approved", Assert.Single(page.Items).Return.Status);
    }
    [Theory]
    [InlineData(null)]
    [InlineData("another-owner")]
    public async Task Unowned_or_other_account_order_is_not_accessible(string? owner)
    {
        var id = await Seed(owner);
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => Request("requester", id, Guid.NewGuid()))).Status);
        await using var scope = fixture.Services.CreateAsyncScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<OrderingDbContext>().ReturnChanges.AnyAsync(x => x.OrderId == id));
    }
    [Fact]
    public async Task Account_cannot_request_a_return_before_delivery()
    {
        var id = await Seed("owner", false);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => Request("owner", id, Guid.NewGuid()))).Status);
    }
}
