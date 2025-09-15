using Microsoft.Extensions.Hosting;
using Serilog.MemoryMapped.Queue.Monitor;

namespace Serilog.MemoryMapped.Sink.Forwarder.WorkerServices;

public sealed class LogMonitorServiceHost(IMemoryMappedQueueMonitor workerService, ILogger logger) : BackgroundService
{
    private Task? runningTask;

    private const int ContinuousRetryIntervalMinutes = 1;
    private readonly TimeSpan continuousRetryTimeSpan = TimeSpan.FromMinutes(ContinuousRetryIntervalMinutes);
    private readonly int monitoringInterval = 10;

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var serviceName = nameof(LogMonitorServiceHost);
        logger.Information("Background Service:{service} is running.", serviceName);

        var combinedPolicy = HostingPolicyBuilder.CreateCombinedRetryPolicy(serviceName, continuousRetryTimeSpan, logger);

        runningTask = combinedPolicy.ExecuteAsync(async (ct) =>
        {
            if (!ct.IsCancellationRequested)
            {
                await workerService.ExecuteAsync(cancellationToken);
                await Task.Delay(monitoringInterval, cancellationToken);
            }
        }, cancellationToken);

        return runningTask;
    }


    public override void Dispose()
    {
        if (runningTask is not null)
        {
            if (runningTask.IsCompleted)
            {
                runningTask.Dispose();
            }

            runningTask = null;
        }

        workerService.Dispose();
        base.Dispose();
    }
}