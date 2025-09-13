namespace Serilog.MemoryMapped.Sink.Forwarder.Configuration;

public class ForwarderOptions
{
    public int MaxMessageLength { get; set; } = 4000;
    public int MaxExceptionLength { get; set; } = 8000;
    public int CommandTimeout { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);
}