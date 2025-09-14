using Serilog.MemoryMapped.Sink.Sinks;

namespace Serilog.MemoryMapped.Sink.Forwarder;

public interface ILogEventForwarder
{
    Task Initialize(CancellationToken cancellationToken);
    Task ForwardAsync(LogEventWrapper entry, CancellationToken cancellationToken);
    Task ForwardBatchAsync(IEnumerable<LogEventWrapper> entries, CancellationToken cancellationToken);
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken);
}