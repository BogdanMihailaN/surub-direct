using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Domain;
using SurubDirect.Ordering.Infrastructure;
using Xunit;

namespace SurubDirect.Tests;
public sealed class AdminPartialReturnFilterTests(OrderingCartFixture fixture) : IClassFixture<OrderingCartFixture>
{
    [Fact]
    public async Task Counts_each_order_once_and_matches_any_of_its_requests_before_paging()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < 28; i++)
        {
            var (order, _) = ReturnSamples.Create();
            db.DemoOrders.Add(order);
            if (i == 27) continue;
            for (var j = 0; j < 2; j++)
            {
                var request = PartialReturnRequest.Create(Guid.NewGuid(), order, [new(order.Lines[0].OfferId, 1)], "Retur demonstrativ", "admin", now);
                if (i == 0 && j == 1) request.Decide(Guid.NewGuid(), 0, "Approved", "Aprobat de magazin", "admin", now);
                db.PartialReturns.Add(request);
            }
        }
        await db.SaveChangesAsync();
        var handler = scope.ServiceProvider.GetRequiredService<ListAdminOrdersHandler>();
        var first = await handler.Handle(null, null, 1, default, partialReturnStatus: " Requested ");
        var second = await handler.Handle(null, null, 2, default, partialReturnStatus: "Requested");
        Assert.Equal(27, first.Total);
        Assert.Equal(25, first.Items.Count);
        Assert.Equal(2, second.Items.Count);
        Assert.Equal(27, first.Items.Concat(second.Items).Select(x => x.Id).Distinct().Count());
        var approved = Assert.Single((await handler.Handle("Confirmed", null, 1, default, "NotRequested", "NotStarted", "Approved")).Items);
        Assert.Single((await handler.Handle(null, approved.Number, 1, default, partialReturnStatus: "Requested")).Items);
        Assert.Empty((await handler.Handle(null, approved.Number, 1, default, paymentStatus: "Paid", partialReturnStatus: "Approved")).Items);
        Assert.Equal(400, (await Assert.ThrowsAsync<BusinessException>(() => handler.Handle(null, null, 1, default, partialReturnStatus: "NotRequested"))).Status);
    }
}
