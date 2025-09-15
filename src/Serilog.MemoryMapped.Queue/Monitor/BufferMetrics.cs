namespace Serilog.MemoryMapped.Queue.Monitor;

public class BufferMetrics
{
    public DateTime Timestamp { get; set; }
    public long MessageCount { get; set; }
    public long AvailableSpace { get; set; }
    public long CapacityBytes { get; set; }
    public double MessagesPerSecond { get; set; }
    public double UsagePercentage { get; set; }
}