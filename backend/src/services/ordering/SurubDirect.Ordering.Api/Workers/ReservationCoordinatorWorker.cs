using SurubDirect.Ordering.Application;

namespace SurubDirect.Ordering.Api.Workers;
public sealed class ReservationCoordinatorWorker(IServiceScopeFactory scopes, ILogger<ReservationCoordinatorWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        do
        {
            try
            {
                for (var i = 0; i < 10; i++)
                {
                    await using var scope = scopes.CreateAsyncScope();
                    if (await scope.ServiceProvider.GetRequiredService<ProcessReservationPlanHandler>().Handle(ct) == 0)
                        break;
                }
            }
            catch (OperationCanceledException)when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception error)
            {
                logger.LogError(error, "Reservation coordinator iteration failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(ct));
    }
}
