namespace Serilog.MemoryMapped.Sink;

public class MemoryMappedQueueStats
{
    public bool Available { get; set; }
    public long MessageCount { get; set; }
    public long AvailableSpace { get; set; }
    public int CapacityMB { get; set; }
}
