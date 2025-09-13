using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace Serilog.MemoryMapped.Sink.Sinks;

public class TestLogEventSink : ILogEventSink
{
    public List<LogEvent> CapturedEvents { get; } = new();

    public void Emit(LogEvent logEvent)
    {
        SelfLog.WriteLine($"Emit");
        CapturedEvents.Add(logEvent);
    }
}