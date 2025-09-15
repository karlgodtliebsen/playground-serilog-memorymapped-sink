using Serilog.MemoryMapped.Queue;
using Serilog.MemoryMapped.Sink.Sinks;

namespace Serilog.MemoryMapped.Sink;


public interface IMemoryMappedQueue : IMemoryMappedQueue<LogEventWrapper>
{

}