using Microsoft.Extensions.Options;
using SurubDirect.Ordering.Api.Configuration;
using SurubDirect.Ordering.Application;

namespace SurubDirect.Ordering.Api.Workers;
public sealed class ExpiredCartCleanupWorker(IServiceScopeFactory scopes, IOptions<ExpiredCartCleanupOptions> configuration, ILogger<ExpiredCartCleanupWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var options = configuration.Value;
        if (!options.Enabled)
            return;
        logger.LogInformation("Expired cart cleanup started. DryRun={DryRun}, GraceDays={GraceDays}, BatchSize={BatchSize}", options.DryRun, options.GraceDays, options.BatchSize);
        try
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    timeout.CancelAfter(TimeSpan.FromSeconds(30));
                    await using var scope = scopes.CreateAsyncScope();
                    var result = await scope.ServiceProvider.GetRequiredService<IExpiredCartCleanup>().Run(options.GraceDays, options.BatchSize, options.DryRun, timeout.Token);
                    logger.LogInformation(new EventId(7400, "ExpiredCartCleanupBatch"), "Expired cart cleanup batch: DryRun={DryRun}, Candidates={Candidates}, Removed={Removed}", result.DryRun, result.Candidates, result.Removed);
                }
                catch (OperationCanceledException)when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception error)
                {
                    logger.LogWarning(new EventId(7401, "ExpiredCartCleanupFailed"), "Expired cart cleanup failed ({ErrorType}); retry next cycle.", error.GetType().Name);
                }

                await Task.Delay(TimeSpan.FromSeconds(options.IntervalSeconds), ct);
            }
        }
        catch (OperationCanceledException)when (ct.IsCancellationRequested)
        {
        }
    }
}
