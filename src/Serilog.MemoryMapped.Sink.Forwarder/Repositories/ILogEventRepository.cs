using Serilog.MemoryMapped.Sink.Sinks;

namespace Serilog.MemoryMapped.Sink.Forwarder.Repositories;

public interface ILogEventRepository
{
    Task CreateTable(CancellationToken cancellationToken);
    Task Add(IEnumerable<LogEventWrapper> entries, CancellationToken cancellationToken);
    IAsyncEnumerable<LogEventWrapper> Find(object? parameters, CancellationToken cancellationToken);

    Task<bool> TestConnection(CancellationToken cancellationToken);

}