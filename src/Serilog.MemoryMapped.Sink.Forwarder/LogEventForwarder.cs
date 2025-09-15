using Serilog.MemoryMapped.Sink.Forwarder.Repositories;
using Serilog.MemoryMapped.Sink.Sinks;

namespace Serilog.MemoryMapped.Sink.Forwarder;

public class LogEventForwarder(ILogEventRepository repository) : ILogEventForwarder
{
    public async Task Initialize(CancellationToken cancellationToken)
    {
        await repository.CreateTable(cancellationToken);
    }

    public async Task ForwardAsync(LogEventWrapper entry, CancellationToken cancellationToken)
    {
        await repository.Add([entry], cancellationToken);
    }

    public async Task ForwardBatchAsync(IEnumerable<LogEventWrapper> entries, CancellationToken cancellationToken)
    {
        await repository.Add(entries, cancellationToken);
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken)
    {
        return await repository.TestConnection(cancellationToken);
    }
}