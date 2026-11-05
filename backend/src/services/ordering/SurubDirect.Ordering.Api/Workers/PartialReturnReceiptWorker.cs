using SurubDirect.Ordering.Application;
namespace SurubDirect.Ordering.Api.Workers;
public sealed class PartialReturnReceiptWorker(IServiceScopeFactory scopes, IWebHostEnvironment environment, ILogger<PartialReturnReceiptWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!environment.IsDevelopment()) return;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        do
        {
            try
            {
                for (var i = 0; i < 10; i++)
                {
                    await using var scope = scopes.CreateAsyncScope();
                    if (await scope.ServiceProvider.GetRequiredService<ProcessPartialReturnReceiptHandler>().Handle(ct) == 0) break;
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }
            catch (Exception error) { logger.LogError(error, "Partial return receipt iteration failed."); }
        } while (await timer.WaitForNextTickAsync(ct));
    }
}
