using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Serilog.MemoryMapped.Sink.Console;

public static class LogProducer
{
    public static void Produce(Action<string> output, Microsoft.Extensions.Logging.ILogger logger)
    {
        using var activity = new Activity("TestOperation")
            .SetIdFormat(ActivityIdFormat.W3C) // Use W3C format
            .Start();

        activity.SetParentId("00-12345678901234567890123456789012-1234567890123456-01");
        // Add some tags to the activity
        activity?.SetTag("test.method", "Test_With_Activity_Tracing");
        activity?.SetTag("test.class", nameof(LogProducer));

        // Create listener for the activity source
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        for (var i = 0; i < 1000; i++)
        {
            using (LogContext.PushProperty("TraceId", Activity.Current?.TraceId.ToString()))
            using (LogContext.PushProperty("SpanId", Activity.Current?.SpanId.ToString()))
            {
                logger.LogTrace("the Verbose message template {UserId} {t1} {t2} {t3} {index}", "the user", "the t1", "the t2", "the t3", i);
                logger.LogDebug("the Debug message template {UserId} {t1} {t2} {t3} {index}", "the user", "the t1", "the t2", "the t3", i);
                logger.LogInformation("the Information message template {UserId} {t1} {t2} {t3} {index}", "the user", "the t1", "the t2", "the t3", i);
                logger.LogError(new FileNotFoundException("No Luck", "the file not found"), "the Information message template {UserId} {t1} {t2} {t3} {index}", "the user", "the t1", "the t2", "the t3", i);
            }

            output($"Emitting LogEntries {i}");
        }

        output("Done Emitting");
    }
}