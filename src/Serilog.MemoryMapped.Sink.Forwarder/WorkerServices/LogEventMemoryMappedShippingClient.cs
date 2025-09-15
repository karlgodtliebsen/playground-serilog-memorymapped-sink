using Serilog.Events;

namespace Serilog.MemoryMapped.Sink.Forwarder.WorkerServices;

public class LogEventMemoryMappedShippingClient(IMemoryMappedQueue memoryMappedQueue, ILogEventForwarder forwarder, ILogger logger) : ILogEventMemoryMappedShippingClient
{
    private readonly int monitoringInterval = 10;
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await forwarder.Initialize(cancellationToken);
            while (!cancellationToken.IsCancellationRequested)
            {
                var entries = memoryMappedQueue.TryDequeueBatch();
                if (logger.IsEnabled(LogEventLevel.Verbose)) logger.Verbose("StartAsync TryDequeue count {count}", entries.Count);
                if (entries.Count > 0)
                {
                    await forwarder.ForwardBatchAsync(entries, cancellationToken);
                }
                await Task.Delay(monitoringInterval, cancellationToken);
            }
        }
        catch (TaskCanceledException)
        {
            if (logger.IsEnabled(LogEventLevel.Verbose)) logger.Verbose("StartAsync method cancelled for LogEvent Shipping Client.");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "An unexpected error occurred in StartAsync LogEvent Shipping Client method.");
            throw;
        }
    }
}