using Serilog.Events;

using System.Diagnostics;

namespace Serilog.MemoryMapped.Sink.Sinks;

public class LogEventWrapper
{
    public LogEventWrapper()
    {
    }

    public LogEventWrapper(DateTimeOffset timeStamp, LogEventLevel level, string renderedMessage, Exception? exception,
        ActivityTraceId? traceId, ActivitySpanId? spanId, IReadOnlyDictionary<string, LogEventPropertyValue> logEventProperties)
    {
        if (exception is not null)
        {
            Exception = exception.ToString();
        }
        Timestamp = timeStamp;
        Level = level.ToString();
        if (traceId.HasValue)
        {
            TraceId = traceId.Value.ToString();
        }
        if (spanId.HasValue)
        {
            SpanId = spanId.Value.ToString();
        }
        RenderedMessage = renderedMessage;
        Properties = logEventProperties.ToJson();
    }

    /// <summary>
    /// The time at which the event occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// The level of the event.
    /// </summary>
    public string Level { get; set; }

    /// <summary>
    /// The id of the trace that was active when the event was created, if any.
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// The id of the span that was active when the event was created, if any.
    /// </summary>
    public string? SpanId { get; set; }

    /// <summary>
    /// The message template describing the event.
    /// </summary>
    public string RenderedMessage { get; init; } = null!;

    /// <summary>
    /// Properties associated with the event, including those presented in <see cref="LogEvent.MessageTemplate"/>.
    /// </summary>
    public string Properties { get; init; }


    /// <summary>
    /// An exception associated with the event, or null.
    /// </summary>
    public string? Exception { get; set; }
}