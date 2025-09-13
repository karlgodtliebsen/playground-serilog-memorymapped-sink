using Microsoft.Extensions.Logging;

using Serilog.Events;
using Serilog.MemoryMapped.Sink.Forwarder.Configuration;
using Serilog.MemoryMapped.Sink.Forwarder.Repositories;

namespace Serilog.MemoryMapped.Sink.Forwarder;

public class LogEventForwarder(ILogEventRepository repository, ILogger<LogEventForwarder> logger) : ILogEventForwarder
{
    private readonly ForwarderOptions forwarderOptions = new();

    public async Task ForwardAsync(LogEvent entry, CancellationToken cancellationToken)
    {
        await repository.Add([entry], cancellationToken);
    }

    public async Task ForwardBatchAsync(IEnumerable<LogEvent> entries, CancellationToken cancellationToken)
    {
        await repository.Add(entries, cancellationToken);
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken)
    {
        return await repository.TestConnection(cancellationToken);
    }
}