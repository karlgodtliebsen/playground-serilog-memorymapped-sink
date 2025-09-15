using System.Text.Json;

namespace Serilog.MemoryMapped.Queue.Serializers;

public class FastJsonSerializer : IFastSerializer
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public ReadOnlySpan<byte> Serialize<T>(T entry) where T : class
    {
        using var writer = new PooledBufferWriter();
        using var jsonWriter = new Utf8JsonWriter(writer);
        JsonSerializer.Serialize(jsonWriter, entry, JsonOpts);
        jsonWriter.Flush();
        var payload = writer.WrittenSpan;
        return payload;
    }

    public T? Deserialize<T>(ReadOnlySpan<byte> buffer) where T : class
    {
        return buffer == Array.Empty<byte>() ? null : JsonSerializer.Deserialize<T>(buffer, JsonOpts);
    }

    public ReadOnlySpan<byte> Serialize2<T>(T entry) where T : class
    {
        var utf8 = JsonSerializer.SerializeToUtf8Bytes(entry, JsonOpts);
        return utf8.AsSpan();
    }

    public T? Deserialize<T>(byte[] buffer) where T : class
    {
        return buffer == Array.Empty<byte>() ? null : JsonSerializer.Deserialize<T>(buffer.AsSpan(), JsonOpts);
    }
}