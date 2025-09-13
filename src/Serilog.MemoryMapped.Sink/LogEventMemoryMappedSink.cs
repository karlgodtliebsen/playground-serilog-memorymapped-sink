using Serilog.Core;
using Serilog.Events;

using System.Diagnostics;

namespace Serilog.MemoryMapped.Sink;

public class LogEventMemoryMappedSink(IMemoryMappedQueue<LogEventWrapper> memoryMappedQueue, LogEventLevel restrictedToMinimumLevel = LogEventLevel.Information) : ILogEventSink
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

            var ms = new MemoryStream();
            var textWriter = new StreamWriter(ms);
            logEvent.RenderMessage(textWriter);

            var result = memoryMappedQueue.TryEnqueue(new LogEventWrapper(logEvent));
            Debug.Assert(result);
        }
        catch (Exception ex)
        {
            PrintError(ex, "Insert Into LogEvent -> LogEventWrapper MemoryMapped File");
        }
    }
}