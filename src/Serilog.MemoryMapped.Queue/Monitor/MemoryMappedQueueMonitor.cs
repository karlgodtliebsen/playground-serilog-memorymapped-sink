using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.MemoryMapped.Queue.Configuration;
using Serilog.MemoryMapped.Sink.Configuration;

namespace Serilog.MemoryMapped.Queue.Monitor;


// Enhanced Memory Mapped Queue Monitor with comprehensive monitoring capabilities

/*
=== WHAT THE MONITOR PROVIDES ===

1. REAL-TIME METRICS:
   - Buffer usage percentage
   - Message count and throughput
   - Available space tracking
   - Growth rate analysis

2. INTELLIGENT ALERTING:
   - High/critical usage warnings
   - Message backlog alerts
   - Low throughput detection (consumer issues)
   - Rapid growth rate alerts

3. HEALTH REPORTING:
   - Historical metrics tracking
   - Health status determination
   - Performance summaries
   - Integration with ASP.NET Core health checks

4. OPERATIONAL INSIGHTS:
   - Identifies when consumers are falling behind
   - Detects buffer capacity issues before they become critical
   - Provides data for capacity planning
   - Helps troubleshoot production issues

5. INTEGRATION READY:
   - Works with logging frameworks
   - Supports health check endpoints
   - Can trigger automated responses
   - Extensible for custom notifications

This monitoring is ESSENTIAL for production logging systems using memory-mapped buffers!
*/

public class MemoryMappedQueueMonitor : IMemoryMappedQueueMonitor
{
    private readonly MemoryMappedQueueBuffer buffer;
    private readonly ILogger<MemoryMappedQueueMonitor> logger;
    private readonly string bufferName;
    private readonly TimeSpan monitoringInterval;
    private readonly TimeSpan alertInterval;
    private readonly MonitoringThresholds thresholds;
    private readonly MonitoringOptions cfg;

    // Track metrics over time
    private readonly Queue<BufferMetrics> metricsHistory = new();
    private DateTime lastAlertTime = DateTime.MinValue;
    private long lastMessageCount = 0;
    private DateTime lastMetricsTime = DateTime.UtcNow;

    public MemoryMappedQueueMonitor(IOptions<MemoryMappedOptions> mmOptions, IOptions<MonitoringOptions> monOptions, ILogger<MemoryMappedQueueMonitor> logger)
    {
        this.bufferName = mmOptions.Value.Name;
        buffer = new MemoryMappedQueueBuffer(bufferName);
        this.logger = logger;
        cfg = monOptions.Value;
        monitoringInterval = cfg.MonitoringInterval;
        alertInterval = cfg.AlertInterval;
        thresholds = cfg.Thresholds;
    }

    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting buffer monitor for: {BufferName}", bufferName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                CollectAndAnalyzeMetrics();
                await Task.Delay(monitoringInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in buffer monitoring for: {BufferName}", bufferName);
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        logger.LogInformation("Buffer monitor stopped for: {BufferName}", bufferName);
    }

    private void CollectAndAnalyzeMetrics()
    {
        var stats = buffer.GetStats();
        if (!stats.Available)
        {
            logger.LogWarning("Buffer {BufferName} is not available for metrics collection", bufferName);
            return;
        }

        var now = DateTime.UtcNow;
        var timeSinceLastMetrics = now - lastMetricsTime;

        // Calculate throughput metrics
        var messagesSinceLastCheck = stats.MessageCount - lastMessageCount;
        var messagesPerSecond = timeSinceLastMetrics.TotalSeconds > 0
            ? messagesSinceLastCheck / timeSinceLastMetrics.TotalSeconds
            : 0;

        var metrics = new BufferMetrics
        {
            Timestamp = now,
            MessageCount = stats.MessageCount,
            AvailableSpace = stats.AvailableSpace,
            CapacityBytes = stats.CapacityMB * 1024 * 1024,
            MessagesPerSecond = messagesPerSecond,
            UsagePercentage = CalculateUsagePercentage(stats)
        };

        // Store metrics history (keep last hour)
        metricsHistory.Enqueue(metrics);
        while (metricsHistory.Count > 0 &&
               (now - metricsHistory.Peek().Timestamp).TotalHours > 1)
        {
            metricsHistory.Dequeue();
        }

        // Log current metrics
        LogCurrentMetrics(metrics);

        // Check for alerts
        CheckAndRaiseAlerts(metrics);

        // Update tracking variables
        lastMessageCount = stats.MessageCount;
        lastMetricsTime = now;
    }

    private void LogCurrentMetrics(BufferMetrics metrics)
    {
        logger.LogInformation(
            "Buffer {BufferName} - Messages: {MessageCount:N0}, " +
            "Usage: {UsagePercentage:F1}%, Available: {AvailableSpace:N0} bytes, " +
            "Throughput: {MessagesPerSecond:F1} msg/sec",
            bufferName, metrics.MessageCount, metrics.UsagePercentage,
            metrics.AvailableSpace, metrics.MessagesPerSecond);
    }

    private void CheckAndRaiseAlerts(BufferMetrics current)
    {
        var now = DateTime.UtcNow;
        var timeSinceLastAlert = now - lastAlertTime;

        // Don't spam alerts - respect alert interval
        if (timeSinceLastAlert < alertInterval)
            return;

        var alerts = new List<string>();

        // High usage alert
        if (current.UsagePercentage > thresholds.HighUsagePercentage)
        {
            alerts.Add($"High buffer usage: {current.UsagePercentage:F1}%");
        }

        // Critical usage alert
        if (current.UsagePercentage > thresholds.CriticalUsagePercentage)
        {
            alerts.Add($"CRITICAL buffer usage: {current.UsagePercentage:F1}%");
        }

        // Large message backlog
        if (current.MessageCount > thresholds.MaxMessageBacklog)
        {
            alerts.Add($"Large message backlog: {current.MessageCount:N0} messages");
        }

        // Low throughput (consumer may be down)
        if (metricsHistory.Count >= 3)
        {
            var recentMetrics = metricsHistory.TakeLast(3).ToList();
            var avgThroughput = recentMetrics.Average(m => m.MessagesPerSecond);

            if (current.MessageCount > 100 && avgThroughput < thresholds.MinThroughputMessagesPerSecond)
            {
                alerts.Add($"Low processing throughput: {avgThroughput:F1} msg/sec (messages accumulating)");
            }
        }

        // Growth rate alert
        var growthRate = CalculateGrowthRate();
        if (growthRate > thresholds.MaxGrowthRatePercentPerMinute)
        {
            alerts.Add($"Rapid buffer growth: {growthRate:F1}% per minute");
        }

        // Raise alerts
        if (alerts.Any())
        {
            foreach (var alert in alerts)
            {
                logger.LogWarning("BUFFER ALERT [{BufferName}]: {Alert}", bufferName, alert);
            }

            // Could also:
            // - Send notifications (email, Slack, etc.)
            // - Write to Windows Event Log
            // - Update health check endpoints
            // - Trigger automated scaling

            lastAlertTime = now;
        }
    }

    private double CalculateUsagePercentage(MemoryMappedQueueStats stats)
    {
        var totalCapacity = stats.CapacityMB * 1024.0 * 1024.0;
        var usedSpace = totalCapacity - stats.AvailableSpace;
        return usedSpace / totalCapacity * 100.0;
    }

    private double CalculateGrowthRate()
    {
        if (metricsHistory.Count < 2)
            return 0;

        var recent = metricsHistory.TakeLast(2).ToList();
        var older = recent[0];
        var newer = recent[1];

        var timeDiff = (newer.Timestamp - older.Timestamp).TotalMinutes;
        if (timeDiff <= 0)
            return 0;

        var usageChange = newer.UsagePercentage - older.UsagePercentage;
        return usageChange / timeDiff; // Percentage points per minute
    }

    // Generate periodic summary reports
    public BufferHealthReport GenerateHealthReport()
    {
        if (!metricsHistory.Any())
            return new BufferHealthReport { BufferName = bufferName, Status = "No Data" };

        var recent = metricsHistory.TakeLast(10).ToList();
        var current = recent.LastOrDefault();

        return new BufferHealthReport
        {
            BufferName = bufferName,
            Status = DetermineHealthStatus(current),
            CurrentMetrics = current,
            AverageUsagePercentage = recent.Average(m => m.UsagePercentage),
            AverageThroughput = recent.Average(m => m.MessagesPerSecond),
            PeakUsagePercentage = recent.Max(m => m.UsagePercentage),
            PeakThroughput = recent.Max(m => m.MessagesPerSecond),
            TotalSamplesCollected = metricsHistory.Count,
            MonitoringDuration = metricsHistory.Any()
                ? DateTime.UtcNow - metricsHistory.First().Timestamp
                : TimeSpan.Zero
        };
    }

    private string DetermineHealthStatus(BufferMetrics? current)
    {
        return current == null
            ? "Unknown"
            : current.UsagePercentage > thresholds.CriticalUsagePercentage
                ? "Critical"
                : current.UsagePercentage > thresholds.HighUsagePercentage
                    ? "Warning"
                    : current.MessageCount > thresholds.MaxMessageBacklog
                        ? "Warning"
                        : "Healthy";
    }

    public void Dispose()
    {
        buffer?.Dispose();
    }
}

// Configuration classes

// Data structures for metrics

/*
// Health check integration for ASP.NET Core
public class MemoryMappedBufferHealthCheck : IHealthCheck
{
    private readonly MemoryMappedQueueMonitor _monitor;
    private readonly MonitoringThresholds _thresholds;

    public MemoryMappedBufferHealthCheck(MemoryMappedQueueMonitor monitor, MonitoringThresholds thresholds)
    {
        _monitor = monitor;
        _thresholds = thresholds;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var report = _monitor.GenerateHealthReport();

            return report.Status switch
            {
                "Healthy" => Task.FromResult(HealthCheckResult.Healthy($"Buffer {report.BufferName} is healthy")),
                "Warning" => Task.FromResult(HealthCheckResult.Degraded($"Buffer {report.BufferName} has warnings: {report.Status}")),
                "Critical" => Task.FromResult(HealthCheckResult.Unhealthy($"Buffer {report.BufferName} is in critical state")),
                _ => Task.FromResult(HealthCheckResult.Unhealthy($"Buffer {report.BufferName} status unknown"))
            };
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Failed to check buffer health", ex));
        }
    }
}

// Usage example with dependency injection
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMemoryMappedQueueMonitoring(
        this IServiceCollection services,
        string bufferName,
        Action<MonitoringOptions> configureOptions = null)
    {
        var options = new MonitoringOptions();
        configureOptions?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<MemoryMappedQueueMonitor>(provider =>
            new MemoryMappedQueueMonitor(
                bufferName,
                provider.GetRequiredService<ILogger<MemoryMappedQueueMonitor>>(),
                options));

        services.AddHostedService<MemoryMappedQueueMonitor>(provider =>
            provider.GetRequiredService<MemoryMappedQueueMonitor>());

        // Add health check
        services.AddHealthChecks()
            .AddCheck<MemoryMappedBufferHealthCheck>("memory-mapped-buffer");

        return services;
    }
}

// Example usage in Program.cs
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add memory-mapped queue monitoring
        builder.Services.AddMemoryMappedQueueMonitoring("MyAppLogBuffer", options =>
        {
            options.MonitoringInterval = TimeSpan.FromSeconds(30);
            options.AlertInterval = TimeSpan.FromMinutes(5);
            options.Thresholds.HighUsagePercentage = 80.0;
            options.Thresholds.CriticalUsagePercentage = 95.0;
            options.Thresholds.MaxMessageBacklog = 5000;
        });

        var app = builder.Build();

        // Health check endpoint
        app.MapHealthChecks("/health");

        // Custom endpoint to get buffer metrics
        app.MapGet("/buffer-health", (MemoryMappedQueueMonitor monitor) =>
        {
            return monitor.GenerateHealthReport();
        });

        app.Run();
    }
}

*/