using Microsoft.Extensions.Options;
using SurubDirect.Ordering.Api.Configuration;
using SurubDirect.Ordering.Application;

namespace SurubDirect.Ordering.Api.Workers;
public sealed class FailedMessageRetentionWorker(IServiceScopeFactory scopes, IOptions<FailedMessageRetentionOptions> configuration, ILogger<FailedMessageRetentionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var options = configuration.Value;
        if (!options.Enabled)
            return;
        logger.LogInformation("Failed-message payload retention started. DryRun={DryRun}, RetentionDays={RetentionDays}, BatchSize={BatchSize}", options.DryRun, options.RetentionDays, options.BatchSize);
        try
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    timeout.CancelAfter(TimeSpan.FromSeconds(30));
                    await using var scope = scopes.CreateAsyncScope();
                    var result = await scope.ServiceProvider.GetRequiredService<IFailedMessageRetention>().Run(options.RetentionDays, options.BatchSize, options.DryRun, timeout.Token);
                    logger.LogInformation(new EventId(7300, "FailedMessageRetentionBatch"), "Failed-message retention batch: DryRun={DryRun}, Candidates={Candidates}, Removed={Removed}", result.DryRun, result.Candidates, result.Removed);
                }
                catch (OperationCanceledException)when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception error)
                {
                    logger.LogWarning(new EventId(7301, "FailedMessageRetentionFailed"), "Failed-message retention failed ({ErrorType}); retry next cycle.", error.GetType().Name);
                }

                await Task.Delay(TimeSpan.FromSeconds(options.IntervalSeconds), ct);
            }
        }
        catch (OperationCanceledException)when (ct.IsCancellationRequested)
        {
        }
    }
}
