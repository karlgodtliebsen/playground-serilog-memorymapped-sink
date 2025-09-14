using Serilog.MemoryMapped.Sink.Sinks;


namespace Serilog.MemoryMapped.Sink;


public class MemoryMappedQueue(string name) : MemoryMappedQueue<LogEventWrapper>(name), IMemoryMappedQueue
{

}