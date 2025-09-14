using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Serilog.MemoryMapped.Sink.Forwarder.WorkerServices;

public sealed class LogEventShippingHostedService(ILogEventMemoryMappedShippingClient workerService, Microsoft.Extensions.Logging.ILogger<LogEventShippingHostedService> logger) : BackgroundService
{
    private Task? runningTask;

    private const int ContinuousRetryIntervalMinutes = 1;
    private readonly TimeSpan continuousRetryTimeSpan = TimeSpan.FromMinutes(ContinuousRetryIntervalMinutes);

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var serviceName = workerService.GetType().FullName ?? "";
        if (logger.IsEnabled(LogLevel.Trace))
            logger.LogTrace("Main Service:{service} with Worker: {worker} is running.", nameof(LogEventShippingHostedService), serviceName);

        var combinedPolicy = HostingPolicyBuilder.CreateCombinedRetryPolicy(serviceName, continuousRetryTimeSpan, logger);

        runningTask = combinedPolicy.ExecuteAsync(async (ct) =>
        {
            if (!ct.IsCancellationRequested)
            {
                await workerService.StartAsync(cancellationToken);
            }
        }, cancellationToken);

        return runningTask;
    }


    public override void Dispose()
    {
        if (logger.IsEnabled(LogLevel.Trace)) logger.LogTrace("Service:{service} with Worker: {worker} is Disposed.", nameof(LogEventShippingHostedService), workerService.GetType().FullName);

        if (runningTask is not null)
        {
            if (runningTask.IsCompleted)
            {
                runningTask.Dispose();
            }

            runningTask = null;
        }

        base.Dispose();
    }
}