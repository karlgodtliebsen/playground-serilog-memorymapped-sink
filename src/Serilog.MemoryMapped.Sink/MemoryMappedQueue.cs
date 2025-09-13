using System.Text.Json;


namespace Serilog.MemoryMapped.Sink;

public class MemoryMappedQueue<T> : IMemoryMappedQueue<T> where T : class
{

    private readonly MemoryMappedQueueBuffer mmBuffer;

    public MemoryMappedQueue(string name)
    {
        mmBuffer = new MemoryMappedQueueBuffer(name);
    }


    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public bool TryEnqueue(T entry)
    {
        // IMPORTANT: payload is a span into 'writer' – don't let it outlive the using scope.
        using var writer = new PooledBufferWriter();
        using var jsonWriter = new Utf8JsonWriter(writer);
        JsonSerializer.Serialize(jsonWriter, entry, JsonOpts);
        jsonWriter.Flush();

        ReadOnlySpan<byte> payload = writer.WrittenSpan;
        if ((uint)payload.Length > ushort.MaxValue) return false;

        // No extra allocations: directly call the span overload.
        return mmBuffer.TryEnqueue(payload);
    }

    //public bool TryEnqueue(T entry)
    //{
    //    // Single allocation for the payload, then pass as span.
    //    byte[] utf8 = JsonSerializer.SerializeToUtf8Bytes(entry, JsonOpts);
    //    if ((uint)utf8.Length > ushort.MaxValue) return false;
    //    return mmBuffer.TryEnqueue(utf8.AsSpan());
    //}


    public T? TryDequeue()
    {
        var messageBytes = mmBuffer.TryDequeue();
        if (messageBytes == Array.Empty<byte>()) return default;
        return JsonSerializer.Deserialize<T>(messageBytes.AsSpan(), JsonOpts);

    }

    public IList<T> TryDequeueBatch(int maxCount = 100)
    {
        var results = new List<T>(Math.Max(0, maxCount));
        for (var i = 0; i < maxCount; i++)
        {
            var entry = TryDequeue();
            if (entry == default) break;
            results.Add(entry);
        }
        return results;
    }

    public MemoryMappedQueueStats GetStats()
    {
        return mmBuffer.GetStats();
    }

    public void Dispose()
    {
        mmBuffer.Dispose();
    }
}

// Simple pooled IBufferWriter<byte>