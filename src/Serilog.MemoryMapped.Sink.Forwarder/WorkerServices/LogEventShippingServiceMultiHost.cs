using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Serilog.MemoryMapped.Sink.Forwarder.WorkerServices;

public sealed class LogEventShippingServiceMultiHost(ILogEventMemoryMappedShippingClient[] workerServices, ILogger<LogEventShippingServiceMultiHost> logger) : BackgroundService
{
    private readonly IList<Task> runningTasks = new List<Task>();
    private const int ContinuousRetryIntervalMinutes = 1;
    private readonly TimeSpan continuousRetryTimeSpan = TimeSpan.FromMinutes(ContinuousRetryIntervalMinutes);

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        foreach (var workerService in workerServices)
        {
            var serviceName = workerService.GetType().FullName ?? "";
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("Background Service:{service} with Worker: {worker} is running.", nameof(LogEventShippingServiceMultiHost), serviceName);

            //TODO: 
            var combinedPolicy = HostingPolicyBuilder.CreateCombinedRetryPolicy(serviceName, continuousRetryTimeSpan, logger);

            var runningTask = combinedPolicy.ExecuteAsync(async (ct) =>
            {
                if (!ct.IsCancellationRequested)
                {
                    await workerService.StartAsync(cancellationToken);
                }
            }, cancellationToken);
            runningTasks.Add(runningTask);
        }
        return runningTasks.First();//not good. get new return strategy
    }


    public override void Dispose()
    {
        foreach (var runningTask in runningTasks)
        {
            if (runningTask.IsCompleted)
            {
                runningTask.Dispose();
            }
        }
        runningTasks.Clear();
        base.Dispose();
    }
}