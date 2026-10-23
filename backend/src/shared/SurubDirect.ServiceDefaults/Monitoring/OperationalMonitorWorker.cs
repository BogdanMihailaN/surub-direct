using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SurubDirect.ServiceDefaults.Monitoring;
public abstract class OperationalMonitorWorker : BackgroundService
{
    private readonly OperationalMonitoringOptions options;
    private readonly TimeProvider clock;
    protected readonly OperationalAlerts Alerts;
    protected OperationalMonitorWorker(IOptions<OperationalMonitoringOptions> options, TimeProvider clock, ILogger logger)
    {
        this.options = options.Value;
        this.clock = clock;
        Alerts = new(logger, TimeSpan.FromSeconds(this.options.ReminderSeconds));
    }

    protected DateTimeOffset Now => clock.GetUtcNow();

    public abstract Task CheckOnce(CancellationToken ct);
    protected async Task Probe(string code, Func<CancellationToken, Task> check, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.ProbeTimeoutSeconds));
        try
        {
            await check(timeout.Token);
            Alerts.Observe(code, false, Now);
        }
        catch (OperationCanceledException)when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // Log only stable codes: exceptions can contain credentials or connection details.
            Alerts.Observe(code, true, Now);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enabled)
            return;
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckOnce(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(options.IntervalSeconds), clock, stoppingToken);
            }
        }
        catch (OperationCanceledException)when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
