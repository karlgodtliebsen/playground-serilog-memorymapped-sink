using Microsoft.Extensions.Options;
using Serilog.MemoryMapped.Queue;
using Serilog.MemoryMapped.Queue.Serializers;
using Serilog.MemoryMapped.Sink.Configuration;
using Serilog.MemoryMapped.Sink.Sinks;


namespace Serilog.MemoryMapped.Sink;

public class MemoryMappedQueue(IOptions<MemoryMappedOptions> options, IFastSerializer serializer) : MemoryMappedQueue<LogEventWrapper>(options.Value.Name, serializer), IMemoryMappedQueue
{

}