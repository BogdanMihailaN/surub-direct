using System.Net;
using System.Net.Http.Json;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Domain;
using SurubDirect.Ordering.Infrastructure;
using Xunit;

namespace SurubDirect.Tests;
public sealed class InventoryReservationsClientTests
{
    [Theory]
    [InlineData("Active", true)]
    [InlineData("Active", false)]
    [InlineData("Unknown", false)]
    public async Task Rejects_mismatched_or_unconfirmed_responses(string status, bool wrongId)
    {
        var plan = ReservationPlan.Create(Guid.NewGuid(), Guid.NewGuid(), [new(Guid.NewGuid(), 2)], DateTimeOffset.UtcNow);
        var client = new InventoryReservationsClient(new StubHttpClientFactory(_ => new(HttpStatusCode.OK) { Content = JsonContent.Create(new { Id = wrongId ? Guid.NewGuid() : plan.Id, plan.OrderId, Status = status, ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15), plan.Lines }) }));
        await Assert.ThrowsAsync<HttpRequestException>(() => client.Release(plan, default));
    }

    [Fact]
    public async Task Conflict_is_distinguished_from_unavailability()
    {
        var plan = ReservationPlan.Create(Guid.NewGuid(), Guid.NewGuid(), [new(Guid.NewGuid(), 2)], DateTimeOffset.UtcNow);
        var rejected = new InventoryReservationsClient(new StubHttpClientFactory(_ => new(HttpStatusCode.Conflict)));
        await Assert.ThrowsAsync<ReservationRejectedException>(() => rejected.Reserve(plan, default));
        var unavailable = new InventoryReservationsClient(new StubHttpClientFactory(_ => new(HttpStatusCode.ServiceUnavailable)));
        await Assert.ThrowsAsync<HttpRequestException>(() => unavailable.Reserve(plan, default));
    }

    [Fact]
    public async Task Consumed_is_preserved_on_reserve_replay_and_release()
    {
        var plan = ReservationPlan.Create(Guid.NewGuid(), Guid.NewGuid(), [new(Guid.NewGuid(), 2)], DateTimeOffset.UtcNow);
        var client = new InventoryReservationsClient(new StubHttpClientFactory(_ => new(HttpStatusCode.OK) { Content = JsonContent.Create(new { plan.Id, plan.OrderId, Status = "Consumed", ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15), plan.Lines }) }));
        Assert.Equal("Consumed", (await client.Reserve(plan, default)).Status);
        Assert.Equal("Consumed", (await client.Release(plan, default)).Status);
    }
}
