using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace Serilog.MemoryMapped.Sink.Forwarder.WorkerServices;

public sealed class LogEventShippingServiceHost(ILogEventMemoryMappedShippingClient workerService, ILogger logger) : BackgroundService
{
    private Task? runningTask;

    private const int ContinuousRetryIntervalMinutes = 1;
    private readonly TimeSpan continuousRetryTimeSpan = TimeSpan.FromMinutes(ContinuousRetryIntervalMinutes);

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var serviceName = workerService.GetType().FullName ?? "";
        if (logger.IsEnabled(LogEventLevel.Verbose)) logger.Verbose("Background Service:{service} with Worker: {worker} is running.", nameof(LogEventShippingServiceHost), serviceName);

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