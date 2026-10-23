using Microsoft.Extensions.Options;
using SurubDirect.Inventory.Api.Configuration;
using SurubDirect.Inventory.Application;

namespace SurubDirect.Inventory.Api.Workers;
public sealed class OutboxRetentionWorker(IServiceScopeFactory scopes, IOptions<OutboxRetentionOptions> configuration, ILogger<OutboxRetentionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var options = configuration.Value;
        if (!options.Enabled)
            return;
        logger.LogInformation("Outbox payload retention started. DryRun={DryRun}, RetentionDays={RetentionDays}, BatchSize={BatchSize}", options.DryRun, options.RetentionDays, options.BatchSize);
        try
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    timeout.CancelAfter(TimeSpan.FromSeconds(30));
                    await using var scope = scopes.CreateAsyncScope();
                    var result = await scope.ServiceProvider.GetRequiredService<IOutboxRetention>().Run(options.RetentionDays, options.BatchSize, options.DryRun, timeout.Token);
                    logger.LogInformation(new EventId(7200, "OutboxRetentionBatch"), "Outbox retention batch: DryRun={DryRun}, Candidates={Candidates}, Removed={Removed}", result.DryRun, result.Candidates, result.Removed);
                }
                catch (OperationCanceledException)when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception error)
                {
                    logger.LogWarning(new EventId(7201, "OutboxRetentionFailed"), "Outbox retention failed ({ErrorType}); retry next cycle.", error.GetType().Name);
                }

                await Task.Delay(TimeSpan.FromSeconds(options.IntervalSeconds), ct);
            }
        }
        catch (OperationCanceledException)when (ct.IsCancellationRequested)
        {
        }
    }
}
