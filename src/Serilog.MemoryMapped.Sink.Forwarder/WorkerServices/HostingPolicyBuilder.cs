using Microsoft.Extensions.Logging;
using Polly;
using Polly.Wrap;

namespace Serilog.MemoryMapped.Sink.Forwarder.WorkerServices;

public static class HostingPolicyBuilder
{
    public static AsyncPolicyWrap CreateCombinedRetryPolicy(string serviceName, TimeSpan continuousRetryTimeSpan, Microsoft.Extensions.Logging.ILogger logger)
    {
        // Initial retry policy: 3 attempts with exponential backoff
        var initialRetryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, context) =>
                    logger.LogWarning(exception, "{serviceName} - Initial retry {retryCount} after {timeSpan}",
                        serviceName, retryCount, timeSpan));

        // Continuous retry policy: retry every minute indefinitely
        var continuousRetryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryForeverAsync(
                retryAttempt => continuousRetryTimeSpan,
                (exception, retryCount, timeSpan) =>
                    logger.LogWarning(exception, "{serviceName} - Continuous retry {retryCount} after {timeSpan}",
                        serviceName, retryCount, timeSpan));

        // Combine the policies: initial retries first, then continuous retries
        return Policy.WrapAsync(continuousRetryPolicy, initialRetryPolicy);
    }
}