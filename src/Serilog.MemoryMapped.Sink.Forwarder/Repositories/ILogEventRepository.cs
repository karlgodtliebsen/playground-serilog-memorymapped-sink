using Serilog.Events;

namespace Serilog.MemoryMapped.Sink.Forwarder.Repositories;

public interface ILogEventRepository
{
    Task CreateTable(CancellationToken cancellationToken);
    Task Add(IEnumerable<LogEvent> entries, CancellationToken cancellationToken);
    IAsyncEnumerable<LogEvent> Find(object? parameters, CancellationToken cancellationToken);

    Task<bool> TestConnection(CancellationToken cancellationToken);

}