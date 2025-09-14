using Microsoft.Extensions.Options;

using Serilog.MemoryMapped.Sink.Configuration;
using Serilog.MemoryMapped.Sink.Sinks;


namespace Serilog.MemoryMapped.Sink;


public class MemoryMappedQueue(IOptions<MemoryMappedOptions> options) : MemoryMappedQueue<LogEventWrapper>(options.Value.Name), IMemoryMappedQueue
{

}