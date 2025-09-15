namespace Serilog.MemoryMapped.Queue.Monitor;

public class MonitoringThresholds
{
    public double HighUsagePercentage { get; set; } = 75.0;
    public double CriticalUsagePercentage { get; set; } = 90.0;
    public long MaxMessageBacklog { get; set; } = 10000;
    public double MinThroughputMessagesPerSecond { get; set; } = 1.0;
    public double MaxGrowthRatePercentPerMinute { get; set; } = 10.0;
}