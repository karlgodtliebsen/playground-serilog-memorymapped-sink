using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting.Compact;

using System.Diagnostics;

namespace Serilog.MemoryMapped.Sink.Sinks;

public class LogEventMemoryMappedSink(IMemoryMappedQueue memoryMappedQueue,
    LogEventLevel restrictedToMinimumLevel = LogEventLevel.Information) : ILogEventSink, IBatchedLogEventSink
{
    private void LogError(Exception ex, string action)
    {
        SelfLog.WriteLine($"Failed {action}\n Exception: {ex}");
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

            using var textWriter = new StringWriter();
            var formatter = new CompactJsonFormatter();
            formatter.Format(logEvent, textWriter);
            var renderedMessage = textWriter.ToString();

            LogEventWrapper logEventWrapper = new LogEventWrapper(logEvent.Timestamp, logEvent.Level, renderedMessage,
                 logEvent.Exception, logEvent.TraceId, logEvent.SpanId, logEvent.Properties);
            var result = memoryMappedQueue.TryEnqueue(logEventWrapper);
            Debug.Assert(result);
            SelfLog.WriteLine("Emitted message");
        }
        catch (Exception ex)
        {
            LogError(ex, "Insert Into LogEvent -> LogEventWrapper MemoryMapped File");
        }
    }

    public Task EmitBatchAsync(IReadOnlyCollection<LogEvent> batch)
    {
        foreach (var logEvent in batch)
        {
            Emit(logEvent);
        }
        return Task.CompletedTask;
    }
}