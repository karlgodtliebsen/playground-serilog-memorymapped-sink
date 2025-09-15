using System.Buffers;
using MemoryPack;

namespace Serilog.MemoryMapped.Serializers;

public class FastMemoryPackSerializer : IFastSerializer
{
    public ReadOnlySpan<byte> Serialize<T>(T entry) where T : class
    {
        var writer = new ArrayBufferWriter<byte>();
        MemoryPackSerializer.Serialize(writer, entry);
        return writer.WrittenSpan;
    }

    public T? Deserialize<T>(ReadOnlySpan<byte> buffer) where T : class
    {
        return MemoryPackSerializer.Deserialize<T>(buffer);
    }
}