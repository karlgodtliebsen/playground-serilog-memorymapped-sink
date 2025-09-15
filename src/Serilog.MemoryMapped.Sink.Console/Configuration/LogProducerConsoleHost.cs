using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.MemoryMapped.Sink.Forwarder.WorkerServices;

namespace Serilog.MemoryMapped.Sink.Console.Configuration;

public sealed class LogProducerConsoleHost(ILogger<LogProducerConsoleHost> logger) : BackgroundService
{
    private Task? runningTask;
    private const int ContinuousRetryIntervalMinutes = 1;
    private readonly TimeSpan continuousRetryTimeSpan = TimeSpan.FromMinutes(ContinuousRetryIntervalMinutes);
    private readonly int monitoringInterval = 10;

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var serviceName = nameof(LogProducerConsoleHost);
        logger.LogInformation("Background Service:{service} is running.", serviceName);

        var combinedPolicy = HostingPolicyBuilder.CreateCombinedRetryPolicy(serviceName, continuousRetryTimeSpan, logger);

        runningTask = combinedPolicy.ExecuteAsync(async (ct) =>
        {
            while (!ct.IsCancellationRequested)
            {
                LogProducer.Produce(System.Console.WriteLine, logger);
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

        base.Dispose();
    }
}