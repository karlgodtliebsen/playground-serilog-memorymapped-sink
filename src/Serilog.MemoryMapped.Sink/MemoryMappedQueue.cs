using Serilog.Parsing;

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;


namespace Serilog.MemoryMapped.Sink;

public class MemoryMappedQueue(string name) : MemoryMappedQueue<LogEventWrapper>(name), IMemoryMappedQueue
{

}
public static class JsonConfig
{
    public static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        //// Configure the type resolver for polymorphic serialization
        //options.TypeInfoResolver = JsonTypeInfoResolver.Combine(
        //    new DefaultJsonTypeInfoResolver
        //    {
        //        Modifiers = { ConfigurePolymorphism }
        //    }
        //);

        return options;
    }

    private static void ConfigurePolymorphism(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type == typeof(MessageTemplateToken))
        {
            typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
            {
                TypeDiscriminatorPropertyName = "$type",
                DerivedTypes =
                {
                    new JsonDerivedType(typeof(PropertyToken), "property"),
                    new JsonDerivedType(typeof(TextToken), "text")
                }
            };
        }
    }
}
public class MemoryMappedQueue<T>(string name) : IMemoryMappedQueue<T> where T : class
{
    private readonly MemoryMappedQueueBuffer mmBuffer = new(name);

    //private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public bool TryEnqueue(T entry)
    {
        // IMPORTANT: payload is a span into 'writer' – don't let it outlive the using scope.
        using var writer = new PooledBufferWriter();
        using var jsonWriter = new Utf8JsonWriter(writer);
        JsonSerializer.Serialize(jsonWriter, entry, JsonConfig.CreateOptions());
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
        if (messageBytes == Array.Empty<byte>()) return null;
        return JsonSerializer.Deserialize<T>(messageBytes.AsSpan(), JsonConfig.CreateOptions());
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

