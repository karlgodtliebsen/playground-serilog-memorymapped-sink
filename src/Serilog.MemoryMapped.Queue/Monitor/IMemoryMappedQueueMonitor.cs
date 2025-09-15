namespace Serilog.MemoryMapped.Queue.Monitor;

public interface IMemoryMappedQueueMonitor : IDisposable
{
    Task ExecuteAsync(CancellationToken stoppingToken);
    BufferHealthReport GenerateHealthReport();
}