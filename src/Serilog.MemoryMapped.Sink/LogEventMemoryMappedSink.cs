using Serilog.Core;
using Serilog.Events;

using System.Diagnostics;

namespace Serilog.MemoryMapped.Sink;

public class LogEventMemoryMappedSink(IMemoryMappedQueue<LogEvent> memoryMappedQueue, LogEventLevel restrictedToMinimumLevel = LogEventLevel.Information) : ILogEventSink
{
    private void PrintError(Exception ex, string action)
    {
        Debug.Print($"Failed {action}\n Exception: {ex}");
        Trace.WriteLine($"Failed {action}\n Exception: {ex}");
        Console.WriteLine($"Failed {action}\n Exception: {ex}");
    }

    public void Emit(LogEvent logEvent)
    {
        try
        {
            if (logEvent.Level < restrictedToMinimumLevel)
            {
                //only log restrictedToMinimumLevel and above
                return;
            }

            var result = memoryMappedQueue.TryEnqueue(logEvent);
            Debug.Assert(result);
        }
        catch (Exception ex)
        {
            PrintError(ex, "Insert Into LogEvent MemoryMapped File");
        }
    }
}