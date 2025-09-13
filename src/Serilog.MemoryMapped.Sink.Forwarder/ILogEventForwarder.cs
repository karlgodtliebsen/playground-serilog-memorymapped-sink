using Serilog.Events;

namespace Serilog.MemoryMapped.Sink.Forwarder;

public interface ILogEventForwarder
{
    Task ForwardAsync(LogEvent entry, CancellationToken cancellationToken);
    Task ForwardBatchAsync(IEnumerable<LogEvent> entries, CancellationToken cancellationToken);
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken);
}