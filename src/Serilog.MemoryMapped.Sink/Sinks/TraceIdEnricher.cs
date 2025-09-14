using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.MemoryMapped.Sink.Sinks;

public class TraceIdEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;
        if (activity != null)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceId", activity.TraceId.ToString()));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SpanId", activity.SpanId.ToString()));

            // Optionally add parent span ID
            if (activity.ParentSpanId != default)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ParentSpanId", activity.ParentSpanId.ToString()));
            }
        }
        else
        {
            // Fallback to generating a correlation ID if no Activity
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceId", Guid.NewGuid().ToString()));
        }
    }
}