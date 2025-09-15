namespace Serilog.MemoryMapped.Queue.Monitor;

public class BufferHealthReport
{
    public string BufferName { get; set; } = null!;
    public string Status { get; set; } = null!;
    public BufferMetrics CurrentMetrics { get; set; } = null!;
    public double AverageUsagePercentage { get; set; }
    public double AverageThroughput { get; set; }
    public double PeakUsagePercentage { get; set; }
    public double PeakThroughput { get; set; }
    public int TotalSamplesCollected { get; set; }
    public TimeSpan MonitoringDuration { get; set; }
}