using Microsoft.Extensions.Logging;

namespace Serilog.MemoryMapped.Sink.Forwarder.WorkerServices;

public class LogEventMemoryMappedShippingClient(IMemoryMappedQueue memoryMappedQueue, ILogEventForwarder forwarder,
    ILogger<LogEventMemoryMappedShippingClient> logger) : ILogEventMemoryMappedShippingClient
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var entries = memoryMappedQueue.TryDequeueBatch();
                if (entries.Count > 0)
                {
                    await forwarder.ForwardBatchAsync(entries, cancellationToken);
                }
                await Task.Delay(10, cancellationToken);//TODO: configure this
            }
        }
        catch (TaskCanceledException)
        {
            if (logger.IsEnabled(LogLevel.Trace)) logger.LogTrace("StartAsync method cancelled for LogEvent Shipping Client.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unexpected error occurred in StartAsync LogEvent Shipping Client method.");
            throw;
        }
    }
}