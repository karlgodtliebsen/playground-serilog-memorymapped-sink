using Serilog.MemoryMapped.Queue.Monitor;

namespace Serilog.MemoryMapped.Queue.Configuration;

public class MonitoringOptions
{
    public TimeSpan MonitoringInterval { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan AlertInterval { get; set; } = TimeSpan.FromMinutes(5);
    public MonitoringThresholds Thresholds { get; set; } = new();
}