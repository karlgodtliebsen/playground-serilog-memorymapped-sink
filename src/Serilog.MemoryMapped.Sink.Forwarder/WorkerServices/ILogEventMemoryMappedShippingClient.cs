namespace Serilog.MemoryMapped.Sink.Forwarder.WorkerServices;

public interface ILogEventMemoryMappedShippingClient
{
    Task StartAsync(CancellationToken cancellationToken);
}