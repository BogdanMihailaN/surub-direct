using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Domain;

namespace SurubDirect.Tests;
public sealed class FakeReservationInventory : IReservationInventory
{
    public bool LoseConsumeResponse { get; set; }
    public int ConsumeCalls { get; private set; }

    public Task<ReservationObservation> Consume(ReservationPlan plan, CancellationToken ct)
    {
        ConsumeCalls++;
        var value = Records[plan.Id];
        if (value.Status == "Active")
            Records[plan.Id] = value with
            {
                Status = "Consumed"
            };
        if (LoseConsumeResponse)
        {
            LoseConsumeResponse = false;
            throw new HttpRequestException("Lost consume response");
        }

        return Task.FromResult(Records[plan.Id]);
    }

    public bool LoseReserveResponse { get; set; }
    public bool LoseReleaseResponse { get; set; }
    public bool Reject { get; set; }
    public int ReserveCalls { get; private set; }
    public int ReleaseCalls { get; private set; }
    public Dictionary<Guid, ReservationObservation> Records { get; } = [];

    public Task<ReservationObservation> Reserve(ReservationPlan plan, CancellationToken ct)
    {
        ReserveCalls++;
        if (Reject)
            throw new ReservationRejectedException();
        if (!Records.ContainsKey(plan.Id))
            Records[plan.Id] = new("Active", DateTimeOffset.UtcNow.AddMinutes(15));
        if (LoseReserveResponse)
        {
            LoseReserveResponse = false;
            throw new HttpRequestException("Lost response");
        }

        return Task.FromResult(Records[plan.Id]);
    }

    public Task<ReservationObservation> Release(ReservationPlan plan, CancellationToken ct)
    {
        ReleaseCalls++;
        Records[plan.Id] = new("Released", DateTimeOffset.UtcNow);
        if (LoseReleaseResponse)
        {
            LoseReleaseResponse = false;
            throw new HttpRequestException("Lost release response");
        }

        return Task.FromResult(Records[plan.Id]);
    }
}
