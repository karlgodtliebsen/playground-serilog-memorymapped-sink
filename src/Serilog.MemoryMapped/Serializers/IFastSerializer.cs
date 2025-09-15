namespace Serilog.MemoryMapped.Serializers;

public interface IFastSerializer
{
    ReadOnlySpan<byte> Serialize<T>(T entry) where T : class;
    T? Deserialize<T>(ReadOnlySpan<byte> buffer) where T : class;
}