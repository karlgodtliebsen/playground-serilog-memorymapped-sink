using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.MemoryMapped.Queue.Monitor;
using Serilog.MemoryMapped.Sink.Forwarder.WorkerServices;

namespace Serilog.MemoryMapped.Sink.Console;

public sealed class LogMonitorServiceHost(IMemoryMappedQueueMonitor workerService, ILogger<LogMonitorServiceHost> logger) : BackgroundService
{
    private Task? runningTask;

    private const int ContinuousRetryIntervalMinutes = 1;
    private readonly TimeSpan continuousRetryTimeSpan = TimeSpan.FromMinutes(ContinuousRetryIntervalMinutes);
    private readonly int monitoringInterval = 10;

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var serviceName = nameof(LogMonitorServiceHost);
        logger.LogInformation("Background Service:{service} is running.", serviceName);

        var combinedPolicy = HostingPolicyBuilder.CreateCombinedRetryPolicy(serviceName, continuousRetryTimeSpan, logger);

        runningTask = combinedPolicy.ExecuteAsync(async (ct) => { await workerService.ExecuteAsync(ct); }, cancellationToken);

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