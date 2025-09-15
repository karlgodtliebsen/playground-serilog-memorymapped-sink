using Serilog.MemoryMapped.Serializers;


namespace Serilog.MemoryMapped;

public class MemoryMappedQueue<T>(string name, IFastSerializer serializer) : IMemoryMappedQueue<T> where T : class
{
    private readonly MemoryMappedQueueBuffer mmBuffer = new(name);


    public bool TryEnqueue(T entry)
    {
        var payload = serializer.Serialize(entry);
        return (uint)payload.Length <= ushort.MaxValue && mmBuffer.TryEnqueue(payload);
    }

    public T? TryDequeue()
    {
        var messageBytes = mmBuffer.TryDequeue();
        return messageBytes == Array.Empty<byte>() ? null : serializer.Deserialize<T>(messageBytes.AsSpan());
    }

    public IList<T> TryDequeueBatch(int maxCount = 100)
    {
        var results = new List<T>(Math.Max(0, maxCount));
        for (var i = 0; i < maxCount; i++)
        {
            var entry = TryDequeue();
            if (entry == null) break;
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

